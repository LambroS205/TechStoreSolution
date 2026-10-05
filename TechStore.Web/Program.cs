using System;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TechStore.Core.Interfaces;
using TechStore.Infrastructure.Data;
using TechStore.Infrastructure.Repositories;
using TechStore.Infrastructure.Services;
using TechStore.Web.Security;

var builder = WebApplication.CreateBuilder(args);

// 1. Cấu hình kết nối CSDL SQL Server với Entity Framework Core 10
// Hàm GetConnectionString("DefaultConnection") tự động đọc mục "DefaultConnection" nằm bên trong khối "ConnectionStrings" của appsettings.json
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Server=(localdb)\\MSSQLLocalDB;Database=TechStoreDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True;";

builder.Services.AddDbContext<TechStoreDbContext>(options =>
{
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorNumbersToAdd: null);
    });
});

// 2. Kích hoạt bộ nhớ đệm MemoryCache & HttpContextAccessor
builder.Services.AddMemoryCache();
builder.Services.AddHttpContextAccessor();

// 3. Đăng ký dịch vụ Kiểm toán, Lưu trữ tập tin & Email, Cấu hình giao diện CMS
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<ISiteSettingsService, SiteSettingsService>();

// Đăng ký Repository & Unit of Work pattern (Clean Architecture)
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// 4. Cấu hình Xác thực bằng Cookie (Cookie Authentication)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "TechStore.AuthSession";
        options.Cookie.HttpOnly = true;
        options.ExpireTimeSpan = TimeSpan.FromDays(14);
        options.LoginPath = "/auth/login";
        options.LogoutPath = "/auth/logout";
        options.AccessDeniedPath = "/auth/login";
        options.SlidingExpiration = true;
    });

// 5. Đăng ký Cơ chế Phân quyền Động Policy-based (Dynamic Permission RBAC)
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

// 6. Cấu hình Rate Limiting — Chống DDoS và Brute-force Attack (phân vùng theo địa chỉ IP)
builder.Services.AddRateLimiter(options =>
{
    // Policy chung: 120 request/phút mỗi IP
    options.AddPolicy("GeneralPolicy", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 120,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));

    // Policy nghiêm ngặt cho Auth endpoints: 10 request/phút mỗi IP
    options.AddPolicy("AuthPolicy", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));

    // Policy cho API endpoints: 60 request/phút mỗi IP
    options.AddPolicy("ApiPolicy", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 60,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.ContentType = "text/html; charset=utf-8";
        await context.HttpContext.Response.WriteAsync(
            "<h2>⚠️ Quá nhiều yêu cầu</h2><p>Bạn đã gửi quá nhiều yêu cầu trong thời gian ngắn. Vui lòng thử lại sau 1 phút.</p>",
            cancellationToken);
    };
});

// 7. Thêm Controllers với Razor Views & Web API Controllers
// Trong .NET 10, Hot Reload đã được tích hợp sẵn mặc định nên không cần gói AddRazorRuntimeCompilation
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});

var app = builder.Build();

// Tự động kiểm tra và chuẩn hóa encoding Unicode UTF-8 tiếng Việt cho SiteSettings và phân quyền
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<TechStoreDbContext>();
    await DbInitializer.InitializeAndRepairEncodingAsync(dbContext);
}

// Pipeline xử lý HTTP Requests
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

// Global Exception Handler — Bắt mọi lỗi chưa xử lý, log chi tiết và trả về trang lỗi thân thiện
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exceptionHandler = context.Features.Get<IExceptionHandlerFeature>();
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();

        if (exceptionHandler?.Error != null)
        {
            logger.LogError(exceptionHandler.Error,
                "Unhandled exception at {Path}: {Message}",
                context.Request.Path, exceptionHandler.Error.Message);
        }

        // Nếu là API request thì trả JSON
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(new { success = false, message = "Đã xảy ra lỗi hệ thống. Vui lòng thử lại sau!" });
        }
        else
        {
            // Chuyển tiếp về trang Error
            context.Response.Redirect("/error/500");
        }
    });
});

// Điều hướng mã trạng thái HTTP (404, 403, 500) sang view tùy chỉnh
app.UseStatusCodePagesWithReExecute("/error/{0}");

app.UseHttpsRedirection();
app.UseStaticFiles();

// Security Headers bảo vệ ứng dụng (chống MIME-sniffing, Clickjacking, XSS)
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "SAMEORIGIN");
    context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    await next();
});

app.UseRouting();

// Rate Limiting Middleware — phải đặt sau UseRouting, trước UseAuthentication
app.UseRateLimiter();

// Thứ tự Middleware bắt buộc: Authentication -> Authorization
app.UseAuthentication();
app.UseAuthorization();

// 8. Định tuyến khu vực Area (Admin Panel) và Storefront (Giao diện mua sắm)
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Map các Web API Controllers
app.MapControllers();

app.Run();
using System;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TechStore.Core.Interfaces;
using TechStore.Infrastructure.Data;
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

// 3. Đăng ký dịch vụ Kiểm toán, Lưu trữ tập tin & Email
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();
builder.Services.AddScoped<IEmailService, EmailService>();

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

// 6. Thêm Controllers với Razor Views & Web API Controllers
// Trong .NET 10, Hot Reload đã được tích hợp sẵn mặc định nên không cần gói AddRazorRuntimeCompilation
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Pipeline xử lý HTTP Requests
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Thứ tự Middleware bắt buộc: Authentication -> Authorization
app.UseAuthentication();
app.UseAuthorization();

// 7. Định tuyến khu vực Area (Admin Panel) và Storefront (Giao diện mua sắm)
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Map các Web API Controllers
app.MapControllers();

app.Run();
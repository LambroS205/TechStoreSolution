using System;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using TechStore.Core.Entities;
using TechStore.Core.Interfaces;
using TechStore.Infrastructure.Data;

namespace TechStore.Infrastructure.Services;

/// <summary>
/// Dịch vụ tự động ghi nhật ký kiểm toán các thao tác của quản trị viên
/// </summary>
public class AuditLogService : IAuditLogService
{
    private readonly TechStoreDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditLogService(TechStoreDbContext context, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task LogAsync(string action, string module, string? recordId, object? oldValues, object? newValues)
    {
        try
        {
            var httpContext = _httpContextAccessor.HttpContext;
            int? userId = null;

            if (httpContext?.User?.Identity?.IsAuthenticated == true)
            {
                var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int uid))
                {
                    userId = uid;
                }
            }

            string? ipAddress = httpContext?.Connection?.RemoteIpAddress?.ToString();

            var log = new AuditLog
            {
                UserId = userId,
                Action = action,
                Module = module,
                RecordId = recordId,
                OldValues = oldValues != null ? JsonSerializer.Serialize(oldValues) : null,
                NewValues = newValues != null ? JsonSerializer.Serialize(newValues) : null,
                IpAddress = ipAddress,
                CreatedAt = DateTime.UtcNow
            };

            await _context.AuditLogs.AddAsync(log);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            // Tránh việc lỗi ghi log làm gián đoạn luồng giao dịch chính
            Console.WriteLine($"[AuditLog Error]: {ex.Message}");
        }
    }
}
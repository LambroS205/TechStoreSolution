using System.Threading.Tasks;

namespace TechStore.Core.Interfaces;

public interface IAuditLogService
{
    /// <summary>
    /// Ghi vết hành động quản trị vào bảng AuditLogs
    /// </summary>
    Task LogAsync(string action, string module, string? recordId, object? oldValues, object? newValues);
}
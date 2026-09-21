using System;

namespace TechStore.Core.Entities;

/// <summary>
/// Thực thể ghi nhận nhật ký kiểm toán hành động quản trị hệ thống
/// </summary>
public class AuditLog
{
    public long LogId { get; set; }
    public int? UserId { get; set; }
    public string Action { get; set; } = string.Empty;       // 'VerifyVietQrPayment', 'UpdatePrice', 'AssignPermission'
    public string Module { get; set; } = string.Empty;       // 'Orders', 'Products', 'Permissions', 'Banners'
    public string? RecordId { get; set; }                    // ID của đối tượng bị tác động (Mã đơn, ID quyền,...)
    public string? OldValues { get; set; }                   // Dữ liệu JSON trạng thái cũ
    public string? NewValues { get; set; }                   // Dữ liệu JSON trạng thái mới
    public string? IpAddress { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation Properties
    public virtual User? User { get; set; }
}
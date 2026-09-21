using System;

namespace TechStore.Core.Entities;

/// <summary>
/// Phân quyền đặc cách từng tài khoản (Cấp quyền thêm hoặc Chặn quyền cụ thể)
/// </summary>
public class UserCustomPermission
{
    public int UserId { get; set; }
    public virtual User User { get; set; } = null!;

    public int PermissionId { get; set; }
    public virtual Permission Permission { get; set; } = null!;

    public bool IsGranted { get; set; } = true; // true: Whitelist, false: Blacklist
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
}
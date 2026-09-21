using System;

namespace TechStore.Core.Entities;

/// <summary>
/// Ma trận phân quyền gán quyền hạn cho từng Role
/// </summary>
public class RolePermission
{
    public int RoleId { get; set; }
    public virtual Role Role { get; set; } = null!;

    public int PermissionId { get; set; }
    public virtual Permission Permission { get; set; } = null!;

    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
}
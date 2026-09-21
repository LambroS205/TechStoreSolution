namespace TechStore.Core.Entities;

/// <summary>
/// Liên kết N-N giữa User và Role
/// </summary>
public class UserRole
{
    public int UserId { get; set; }
    public virtual User User { get; set; } = null!;

    public int RoleId { get; set; }
    public virtual Role Role { get; set; } = null!;
}
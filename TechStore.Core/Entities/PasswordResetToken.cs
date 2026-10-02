using System;

namespace TechStore.Core.Entities;

/// <summary>
/// Thực thể lưu trữ mã thông báo khôi phục mật khẩu (Password Reset Token) có thời hạn hiệu lực
/// </summary>
public class PasswordResetToken
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public bool IsUsed { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation Property
    public virtual User User { get; set; } = null!;
}

using System;
using System.Collections.Generic;

namespace TechStore.Core.Entities;

/// <summary>
/// Thực thể giỏ hàng lưu trữ vĩnh viễn trong CSDL gắn với từng User tài khoản
/// </summary>
public class Cart
{
    public int CartId { get; set; }
    public int UserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation Properties
    public virtual User User { get; set; } = null!;
    public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
}
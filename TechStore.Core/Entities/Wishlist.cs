using System;

namespace TechStore.Core.Entities;

/// <summary>
/// Thực thể danh sách sản phẩm yêu thích của khách hàng
/// </summary>
public class Wishlist
{
    public int UserId { get; set; }
    public int ProductId { get; set; }
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;

    // Navigation Properties
    public virtual User User { get; set; } = null!;
    public virtual Product Product { get; set; } = null!;
}

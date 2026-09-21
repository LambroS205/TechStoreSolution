using System;

namespace TechStore.Core.Entities;

/// <summary>
/// Chi tiết từng biến thể sản phẩm nằm trong giỏ hàng Database của User
/// </summary>
public class CartItem
{
    public int CartItemId { get; set; }
    public int CartId { get; set; }
    public int VariantId { get; set; }
    public int Quantity { get; set; } = 1;
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;

    // Navigation Properties
    public virtual Cart Cart { get; set; } = null!;
    public virtual ProductVariant Variant { get; set; } = null!;
}
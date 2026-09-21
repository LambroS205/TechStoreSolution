using System;

namespace TechStore.Core.Entities;

/// <summary>
/// Biến thể sản phẩm vật lý thực tế có SKU, giá bán và số lượng tồn kho riêng biệt
/// </summary>
public class ProductVariant
{
    public int VariantId { get; set; }
    public int ProductId { get; set; }

    public string SKU { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public string VariantName { get; set; } = string.Empty; // Ví dụ: "iPhone 16 Pro Max 256GB Titan Sa Mạc"

    public decimal OriginalPrice { get; set; }
    public decimal SalePrice { get; set; }
    public int StockQuantity { get; set; } = 0;
    public int WeightGrams { get; set; } = 200;
    public string? ThumbnailImage { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation Properties
    public virtual Product Product { get; set; } = null!;
}
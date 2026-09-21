namespace TechStore.Core.Entities;

/// <summary>
/// Chi tiết từng mặt hàng trong đơn hàng lưu snapshot giá tại thời điểm đặt
/// </summary>
public class OrderDetail
{
    public int OrderDetailId { get; set; }
    public int OrderId { get; set; }
    public int VariantId { get; set; }

    public string ProductName { get; set; } = string.Empty;
    public string VariantName { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }

    // Navigation Properties
    public virtual Order Order { get; set; } = null!;
    public virtual ProductVariant Variant { get; set; } = null!;
}
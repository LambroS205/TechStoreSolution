using System;

namespace TechStore.Core.Entities;

/// <summary>
/// Thực thể ghi nhận lịch sử biến động xuất nhập tồn kho
/// </summary>
public class InventoryTransaction
{
    public int InventoryTxId { get; set; }
    public int VariantId { get; set; }
    public int? SupplierId { get; set; }
    public string TransactionType { get; set; } = "IMPORT"; // IMPORT, EXPORT_ORDER, ADJUSTMENT
    public int Quantity { get; set; }
    public decimal? UnitPrice { get; set; }
    public string? ReferenceCode { get; set; }
    public string? Note { get; set; }
    public int CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation Properties
    public virtual ProductVariant Variant { get; set; } = null!;
    public virtual User User { get; set; } = null!;
}

using System;

namespace TechStore.Core.Entities;

/// <summary>
/// Thực thể ghi nhận lịch sử biến động trạng thái đơn hàng
/// </summary>
public class OrderStatusHistory
{
    public int HistoryId { get; set; }
    public int OrderId { get; set; }
    public string? PreviousStatus { get; set; }
    public string NewStatus { get; set; } = string.Empty;
    public string? Note { get; set; }
    public int? ChangedBy { get; set; }
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

    // Navigation Properties
    public virtual Order Order { get; set; } = null!;
    public virtual User? User { get; set; }
}

using System;

namespace TechStore.Core.Entities;

/// <summary>
/// Thực thể đánh giá và nhận xét sản phẩm từ khách hàng
/// </summary>
public class ProductReview
{
    public int ReviewId { get; set; }
    public int ProductId { get; set; }
    public int UserId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public bool IsApproved { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation Properties
    public virtual Product Product { get; set; } = null!;
    public virtual User User { get; set; } = null!;
}

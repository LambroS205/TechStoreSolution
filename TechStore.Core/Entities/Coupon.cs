using System;

namespace TechStore.Core.Entities;

/// <summary>
/// Thực thể mã giảm giá khuyến mãi (Coupon/Voucher)
/// </summary>
public class Coupon
{
    public int CouponId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string DiscountType { get; set; } = "FixedAmount"; // "FixedAmount" hoặc "Percentage"
    public decimal DiscountValue { get; set; }
    public decimal MinOrderAmount { get; set; } = 0;
    public decimal? MaxDiscountAmount { get; set; }
    public int UsageLimit { get; set; } = 100;
    public int UsageCount { get; set; } = 0;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; } = true;
}
using System;
using TechStore.Core.Entities;
using TechStore.Core.Enums;

namespace TechStore.Core.Common;

/// <summary>
/// Bộ tính toán và xác thực mã giảm giá Coupon tập trung
/// </summary>
public static class CouponCalculator
{
    /// <summary>
    /// Kiểm tra tính hợp lệ của mã giảm giá
    /// </summary>
    public static bool TryValidate(Coupon? coupon, decimal subTotal, DateTime now, out string errorMessage)
    {
        if (coupon == null || !coupon.IsActive)
        {
            errorMessage = "Mã giảm giá không tồn tại hoặc đã bị vô hiệu hóa!";
            return false;
        }

        if (coupon.StartDate > now || coupon.EndDate < now)
        {
            errorMessage = "Mã giảm giá chưa đến ngày áp dụng hoặc đã hết hạn!";
            return false;
        }

        if (coupon.UsageCount >= coupon.UsageLimit)
        {
            errorMessage = "Mã giảm giá đã hết lượt sử dụng!";
            return false;
        }

        if (subTotal < coupon.MinOrderAmount)
        {
            errorMessage = $"Mã này chỉ áp dụng cho đơn hàng từ {coupon.MinOrderAmount:N0} đ trở lên!";
            return false;
        }

        errorMessage = string.Empty;
        return true;
    }

    /// <summary>
    /// Tính toán số tiền được giảm giá theo cấu hình của Coupon
    /// </summary>
    public static decimal CalculateDiscount(Coupon coupon, decimal subTotal)
    {
        if (subTotal <= 0) return 0;

        decimal discount;
        if (string.Equals(coupon.DiscountType, DiscountTypes.Percentage, StringComparison.OrdinalIgnoreCase))
        {
            discount = (subTotal * coupon.DiscountValue) / 100m;
            if (coupon.MaxDiscountAmount.HasValue && discount > coupon.MaxDiscountAmount.Value)
            {
                discount = coupon.MaxDiscountAmount.Value;
            }
        }
        else
        {
            discount = coupon.DiscountValue;
        }

        // Không bao giờ giảm vượt quá giá trị đơn hàng
        return Math.Min(discount, subTotal);
    }
}

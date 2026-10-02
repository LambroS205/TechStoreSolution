namespace TechStore.Core.Enums;

/// <summary>
/// Phương thức thanh toán được hỗ trợ
/// </summary>
public static class PaymentMethodNames
{
    public const string COD = "COD";
    public const string VietQR = "VietQR";

    public static string ToDisplayName(string? method)
    {
        return (method ?? "").Trim().ToUpperInvariant() switch
        {
            "VIETQR" => "Chuyển khoản VietQR",
            "COD" => "Thanh toán khi nhận hàng (COD)",
            _ => method ?? "Chưa xác định"
        };
    }
}

/// <summary>
/// Loại hình giảm giá của Coupon
/// </summary>
public static class DiscountTypes
{
    public const string Percentage = "Percentage";
    public const string FixedAmount = "FixedAmount";
}

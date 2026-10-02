namespace TechStore.Core.Enums;

/// <summary>
/// Trạng thái thanh toán của đơn hàng
/// </summary>
public enum PaymentStatus
{
    Pending = 0,    // Chờ thanh toán
    Paid = 1,       // Đã thanh toán thành công
    Failed = 2,     // Thanh toán thất bại
    Refunded = 3    // Đã hoàn tiền
}

public static class PaymentStatusExtensions
{
    public static string ToDisplayName(this string? status)
    {
        return (status ?? "").Trim().ToLowerInvariant() switch
        {
            "pending" => "Chờ thanh toán",
            "paid" => "Đã thanh toán",
            "failed" => "Thất bại",
            "refunded" => "Đã hoàn tiền",
            _ => status ?? "Chưa rõ"
        };
    }

    public static string ToBadgeClass(this string? status)
    {
        return (status ?? "").Trim().ToLowerInvariant() switch
        {
            "paid" => "bg-emerald-50 text-emerald-700 border-emerald-200",
            "pending" => "bg-amber-50 text-amber-700 border-amber-200",
            "failed" => "bg-rose-50 text-rose-700 border-rose-200",
            "refunded" => "bg-purple-50 text-purple-700 border-purple-200",
            _ => "bg-slate-50 text-slate-700 border-slate-200"
        };
    }
}

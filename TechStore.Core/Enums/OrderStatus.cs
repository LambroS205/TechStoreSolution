namespace TechStore.Core.Enums;

/// <summary>
/// Trạng thái tiến trình của đơn hàng
/// </summary>
public enum OrderStatus
{
    Pending = 0,    // Chờ tiếp nhận / chờ xác nhận
    Processing = 1, // Đang đóng gói / xử lý tại kho
    Shipping = 2,   // Đang vận chuyển giao hàng
    Delivered = 3,  // Đã giao hàng thành công
    Cancelled = 4   // Đã hủy đơn
}

public static class OrderStatusExtensions
{
    public static string ToDisplayName(this string? status)
    {
        return (status ?? "").Trim().ToLowerInvariant() switch
        {
            "pending" => "Chờ xử lý",
            "processing" => "Đang đóng gói",
            "shipping" => "Đang giao hàng",
            "delivered" => "Đã giao thành công",
            "cancelled" => "Đã hủy",
            _ => status ?? "Không xác định"
        };
    }

    public static string ToBadgeClass(this string? status)
    {
        return (status ?? "").Trim().ToLowerInvariant() switch
        {
            "pending" => "bg-amber-50 text-amber-700 border-amber-200",
            "processing" => "bg-blue-50 text-blue-700 border-blue-200",
            "shipping" => "bg-indigo-50 text-indigo-700 border-indigo-200",
            "delivered" => "bg-emerald-50 text-emerald-700 border-emerald-200",
            "cancelled" => "bg-rose-50 text-rose-700 border-rose-200",
            _ => "bg-slate-50 text-slate-700 border-slate-200"
        };
    }
}

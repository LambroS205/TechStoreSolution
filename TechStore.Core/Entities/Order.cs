using System;
using System.Collections.Generic;

namespace TechStore.Core.Entities;

/// <summary>
/// Thực thể đơn đặt hàng chính hỗ trợ cả phương thức COD và VietQR
/// </summary>
public class Order
{
    public int OrderId { get; set; }
    public string OrderCode { get; set; } = string.Empty; // Ví dụ: ORD20260921-0088
    public int? UserId { get; set; }

    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string? CustomerEmail { get; set; }
    public string ShippingAddress { get; set; } = string.Empty;
    public string? OrderNotes { get; set; }

    public int? CouponId { get; set; }
    public decimal DiscountAmount { get; set; } = 0;
    public decimal SubTotal { get; set; }
    public decimal ShippingFee { get; set; } = 0;
    public decimal TotalAmount { get; set; }

    public string PaymentMethod { get; set; } = "COD"; // "COD" hoặc "VietQR"
    public string PaymentStatus { get; set; } = "Pending"; // "Pending", "Paid", "Failed", "Refunded"
    public string OrderStatus { get; set; } = "Pending";   // "Pending", "Confirmed", "Processing", "Shipping", "Delivered", "Cancelled"

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation Properties
    public virtual User? User { get; set; }
    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
    public virtual ICollection<PaymentTransaction> PaymentTransactions { get; set; } = new List<PaymentTransaction>();
}
using System;

namespace TechStore.Core.Entities;

/// <summary>
/// Nhật ký giao dịch đối soát thanh toán ngân hàng (Đặc biệt cho chuyển khoản VietQR)
/// </summary>
public class PaymentTransaction
{
    public int TransactionId { get; set; }
    public int OrderId { get; set; }

    public string PaymentMethod { get; set; } = "VietQR";
    public string? TransactionReference { get; set; } // Mã tham chiếu đối soát ngân hàng
    public decimal Amount { get; set; }
    public string Status { get; set; } = "Initialized"; // "Initialized", "Success", "Failed"
    public string? BankCode { get; set; }

    public int? ConfirmedBy { get; set; } // UserId của nhân viên/Admin bấm duyệt
    public DateTime? ConfirmedAt { get; set; }
    public string? RawPayload { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual Order Order { get; set; } = null!;
    public virtual User? ConfirmedByUser { get; set; }
}
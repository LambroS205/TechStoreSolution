using System.Threading.Tasks;
using TechStore.Core.Entities;

namespace TechStore.Core.Interfaces;

public interface IEmailService
{
    /// <summary>
    /// Gửi email xác nhận đặt hàng kèm hóa đơn chi tiết và mã VietQR thanh toán
    /// </summary>
    Task SendOrderConfirmationEmailAsync(Order order);

    /// <summary>
    /// Gửi email chứa liên kết khôi phục mật khẩu cho người dùng
    /// </summary>
    Task SendPasswordResetEmailAsync(string toEmail, string fullName, string resetLink);
}

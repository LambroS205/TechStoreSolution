using System.Threading.Tasks;
using TechStore.Core.Entities;

namespace TechStore.Core.Interfaces;

public interface IEmailService
{
    /// <summary>
    /// Gửi email xác nhận đặt hàng kèm hóa đơn chi tiết và mã VietQR thanh toán
    /// </summary>
    Task SendOrderConfirmationEmailAsync(Order order);
}

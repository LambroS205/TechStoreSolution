using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using TechStore.Core.Entities;
using TechStore.Core.Interfaces;

namespace TechStore.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, IWebHostEnvironment env, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _env = env;
        _logger = logger;
    }

    public async Task SendOrderConfirmationEmailAsync(Order order)
    {
        try
        {
            string htmlContent = GenerateOrderHtml(order);

            // 1. Luôn lưu bản sao Email HTML cục bộ vào wwwroot/uploads/emails/ để kiểm tra offline
            string emailDir = Path.Combine(_env.WebRootPath, "uploads", "emails");
            if (!Directory.Exists(emailDir))
            {
                Directory.CreateDirectory(emailDir);
            }

            string filePath = Path.Combine(emailDir, $"{order.OrderCode}.html");
            await File.WriteAllTextAsync(filePath, htmlContent, Encoding.UTF8);
            _logger.LogInformation("Order confirmation email generated and saved locally: {FilePath}", filePath);

            // 2. Nếu có cấu hình SMTP thực tế, tiến hành gửi email
            string? smtpHost = _configuration["Smtp:Host"];
            if (!string.IsNullOrEmpty(smtpHost) && !string.IsNullOrEmpty(order.CustomerEmail))
            {
                int port = int.TryParse(_configuration["Smtp:Port"], out int p) ? p : 587;
                string? user = _configuration["Smtp:Username"];
                string? pass = _configuration["Smtp:Password"];
                string fromEmail = _configuration["Smtp:FromEmail"] ?? "no-reply@techstore.vn";
                string fromName = _configuration["Smtp:FromName"] ?? "TechStore Siêu Thị Công Nghệ";

                using var client = new SmtpClient(smtpHost, port)
                {
                    Credentials = new NetworkCredential(user, pass),
                    EnableSsl = true
                };

                using var mailMessage = new MailMessage
                {
                    From = new MailAddress(fromEmail, fromName),
                    Subject = $"[TechStore] Xác Nhận Đơn Hàng Thành Công #{order.OrderCode}",
                    Body = htmlContent,
                    IsBodyHtml = true
                };
                mailMessage.To.Add(order.CustomerEmail);

                await client.SendMailAsync(mailMessage);
                _logger.LogInformation("Order confirmation email sent via SMTP to {Email}", order.CustomerEmail);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send order confirmation email for Order {OrderCode}", order.OrderCode);
        }
    }

    private string GenerateOrderHtml(Order order)
    {
        var sb = new StringBuilder();
        sb.Append(@"
<!DOCTYPE html>
<html lang=""vi"">
<head>
    <meta charset=""UTF-8"">
    <title>Xác Nhận Đơn Hàng TechStore</title>
    <style>
        body { font-family: 'Segoe UI', Helvetica, Arial, sans-serif; background-color: #f4f6f9; margin: 0; padding: 20px; color: #334155; }
        .container { max-width: 650px; margin: 0 auto; background-color: #ffffff; border-radius: 16px; overflow: hidden; box-shadow: 0 4px 20px rgba(0,0,0,0.06); }
        .header { background-color: #0046be; color: #ffffff; padding: 24px; text-align: center; }
        .header h1 { margin: 0; font-size: 24px; font-weight: 900; }
        .header p { margin: 4px 0 0 0; font-size: 13px; color: #fff200; font-weight: bold; }
        .content { padding: 28px; }
        .order-badge { display: inline-block; background-color: #eff6ff; color: #1d4ed8; padding: 6px 14px; border-radius: 20px; font-weight: bold; font-size: 12px; margin-bottom: 16px; }
        table { width: 100%; border-collapse: collapse; margin: 20px 0; font-size: 13px; }
        th { background-color: #f8fafc; color: #64748b; padding: 12px 10px; text-align: left; font-size: 11px; text-transform: uppercase; border-bottom: 2px solid #e2e8f0; }
        td { padding: 12px 10px; border-bottom: 1px solid #f1f5f9; }
        .total-row td { font-weight: bold; font-size: 15px; color: #0f172a; border-top: 2px solid #cbd5e1; }
        .vietqr-box { background-color: #eef2ff; border: 1px solid #c7d2fe; border-radius: 12px; padding: 20px; text-align: center; margin: 24px 0; }
        .vietqr-box img { max-width: 220px; border-radius: 8px; border: 1px solid #e0e7ff; }
        .bank-details { margin-top: 14px; font-size: 13px; line-height: 1.6; text-align: left; background: #ffffff; padding: 14px; border-radius: 8px; }
        .footer { background-color: #f8fafc; padding: 20px; text-align: center; font-size: 12px; color: #94a3b8; border-top: 1px solid #e2e8f0; }
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>TechStore Electronics</h1>
            <p>HỆ THỐNG BÁN LẺ THIẾT BỊ CÔNG NGHỆ CHÍNH HÃNG</p>
        </div>
        <div class=""content"">
            <div class=""order-badge"">MÃ ĐƠN: " + order.OrderCode + @"</div>
            <h2 style=""margin-top:0;font-size:20px;color:#0f172a;"">Kính chào " + (order.CustomerName ?? "Quý khách") + @",</h2>
            <p style=""font-size:14px;line-height:1.6;"">
                Cảm ơn bạn đã đặt hàng tại TechStore! Đơn hàng của bạn đã được tiếp nhận thành công và đang được chuẩn bị để đóng gói, vận chuyển trong thời gian sớm nhất.
            </p>

            <h3 style=""font-size:15px;margin-top:24px;border-bottom:1px solid #e2e8f0;padding-bottom:6px;"">Chi Tiết Đơn Hàng</h3>
            <table>
                <thead>
                    <tr>
                        <th>Sản Phẩm</th>
                        <th>Cấu Hình / SKU</th>
                        <th style=""text-align:center;"">SL</th>
                        <th style=""text-align:right;"">Đơn Giá</th>
                    </tr>
                </thead>
                <tbody>");

        foreach (var item in order.OrderDetails)
        {
            sb.Append($@"
                    <tr>
                        <td><strong>{item.ProductName}</strong></td>
                        <td style=""color:#64748b;"">{item.VariantName} ({item.SKU})</td>
                        <td style=""text-align:center;"">{item.Quantity}</td>
                        <td style=""text-align:right;font-weight:bold;color:#2563eb;"">{item.UnitPrice:N0} đ</td>
                    </tr>");
        }

        sb.Append($@"
                    <tr>
                        <td colspan=""3"" style=""text-align:right;color:#64748b;"">Tạm tính:</td>
                        <td style=""text-align:right;font-weight:bold;"">{order.SubTotal:N0} đ</td>
                    </tr>
                    <tr>
                        <td colspan=""3"" style=""text-align:right;color:#64748b;"">Phí vận chuyển toàn quốc:</td>
                        <td style=""text-align:right;font-weight:bold;color:#16a34a;"">MIỄN PHÍ</td>
                    </tr>");

        if (order.DiscountAmount > 0)
        {
            sb.Append($@"
                    <tr>
                        <td colspan=""3"" style=""text-align:right;color:#dc2626;"">Giảm giá Coupon:</td>
                        <td style=""text-align:right;font-weight:bold;color:#dc2626;"">-{order.DiscountAmount:N0} đ</td>
                    </tr>");
        }

        sb.Append($@"
                    <tr class=""total-row"">
                        <td colspan=""3"" style=""text-align:right;"">TỔNG THANH TOÁN:</td>
                        <td style=""text-align:right;color:#dc2626;font-size:18px;"">{order.TotalAmount:N0} đ</td>
                    </tr>
                </tbody>
            </table>

            <h3 style=""font-size:15px;margin-top:24px;border-bottom:1px solid #e2e8f0;padding-bottom:6px;"">Thông Tin Giao Nhận</h3>
            <p style=""font-size:13px;line-height:1.7;margin:0;"">
                <strong>Người nhận:</strong> {order.CustomerName}<br>
                <strong>Số điện thoại:</strong> {order.CustomerPhone}<br>
                <strong>Địa chỉ giao hàng:</strong> {order.ShippingAddress}<br>
                <strong>Phương thức thanh toán:</strong> {(order.PaymentMethod == "VietQR" ? "Chuyển khoản VietQR NAPAS 247" : "Thanh toán khi nhận hàng (COD)")}
            </p>");

        if (order.PaymentMethod == "VietQR" && order.PaymentStatus == "Pending")
        {
            string vietQrUrl = $"https://api.vietqr.io/image/970422-0909123456-compact2.png?amount={(long)order.TotalAmount}&addInfo={order.OrderCode}&accountName=CONG%20TY%20TECHSTORE";
            sb.Append($@"
            <div class=""vietqr-box"">
                <h4 style=""margin:0 0 10px 0;color:#3730a3;font-size:15px;"">Mã Chuyển Khoản Tức Thì VietQR</h4>
                <img src=""{vietQrUrl}"" alt=""VietQR"" />
                <div class=""bank-details"">
                    <strong>Ngân hàng:</strong> MBBank (Ngân hàng Quân Đội)<br>
                    <strong>Số tài khoản:</strong> <span style=""font-family:monospace;font-weight:bold;font-size:14px;color:#1e40af;"">0909123456</span><br>
                    <strong>Chủ tài khoản:</strong> CONG TY TECHSTORE<br>
                    <strong>Số tiền:</strong> <span style=""color:#dc2626;font-weight:bold;"">{order.TotalAmount:N0} đ</span><br>
                    <strong>Nội dung CK:</strong> <span style=""background:#fef08a;padding:2px 8px;border-radius:4px;font-family:monospace;font-weight:bold;"">{order.OrderCode}</span>
                </div>
            </div>");
        }

        sb.Append(@"
            <p style=""font-size:13px;color:#64748b;margin-top:24px;"">
                Mọi thắc mắc xin vui lòng liên hệ tổng đài hỗ trợ miễn phí <strong>1800 6868</strong> hoặc email về <a href=""mailto:support@techstore.vn"">support@techstore.vn</a>.
            </p>
        </div>
        <div class=""footer"">
            &copy; " + DateTime.UtcNow.Year + @" TechStore Electronics. Tiêu chuẩn chuỗi cửa hàng Best Buy. Mọi quyền được bảo lưu.
        </div>
    </div>
</body>
</html>");

        return sb.ToString();
    }
}

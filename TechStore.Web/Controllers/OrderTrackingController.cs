using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using TechStore.Core.Entities;
using TechStore.Core.Interfaces;
using TechStore.Infrastructure.Data;

namespace TechStore.Web.Controllers;

public class OrderTrackingController : Controller
{
    private readonly TechStoreDbContext _context;
    private readonly ISiteSettingsService _siteSettingsService;
    private readonly IConfiguration _configuration;

    public OrderTrackingController(
        TechStoreDbContext context,
        ISiteSettingsService siteSettingsService,
        IConfiguration configuration)
    {
        _context = context;
        _siteSettingsService = siteSettingsService;
        _configuration = configuration;
    }

    /// <summary>
    /// Trang tra cứu thông tin và tiến trình đơn hàng công khai (không cần đăng nhập)
    /// Hỗ trợ cả 2 đường dẫn /order/tracking và /tra-cuu-don-hang
    /// </summary>
    [HttpGet]
    [Route("order/tracking")]
    [Route("tra-cuu-don-hang")]
    public async Task<IActionResult> Index(string? orderCode, string? phone)
    {
        ViewBag.OrderCode = orderCode?.Trim();
        ViewBag.Phone = phone?.Trim();

        if (string.IsNullOrWhiteSpace(orderCode) || string.IsNullOrWhiteSpace(phone))
        {
            return View(null);
        }

        string cleanCode = orderCode.Trim().ToUpperInvariant();
        string cleanPhone = phone.Trim();

        var order = await _context.Orders
            .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Variant)
            .Include(o => o.StatusHistories)
            .AsSplitQuery()
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.OrderCode == cleanCode && o.CustomerPhone == cleanPhone);

        if (order == null)
        {
            ViewBag.ErrorMessage = "Không tìm thấy đơn hàng phù hợp với Mã đơn hàng và Số điện thoại đã nhập. Vui lòng kiểm tra lại!";
            return View(null);
        }

        // Nếu đơn hàng thanh toán VietQR và chưa thanh toán, sinh mã QR để khách thanh toán
        if (order.PaymentMethod == "VietQR" && order.PaymentStatus == "Pending" && order.OrderStatus != "Cancelled")
        {
            var bankId = await _siteSettingsService.GetValueAsync("VietQr.BankId", _configuration["VietQrSettings:BankId"] ?? "970422");
            var accountNo = await _siteSettingsService.GetValueAsync("VietQr.AccountNo", _configuration["VietQrSettings:AccountNo"] ?? "0869162534");
            var accountName = await _siteSettingsService.GetValueAsync("VietQr.AccountName", _configuration["VietQrSettings:AccountName"] ?? "CONG TY TNHH TECHSTORE VIET NAM");
            var template = await _siteSettingsService.GetValueAsync("VietQr.Template", _configuration["VietQrSettings:Template"] ?? "compact2");
            var bankName = await _siteSettingsService.GetValueAsync("VietQr.BankName", _configuration["VietQrSettings:BankName"] ?? "MBBank (Ngân hàng Quân Đội)");

            string vietQrUrl = $"https://img.vietqr.io/image/{bankId}-{accountNo}-{template}.png?amount={(long)order.TotalAmount}&addInfo={order.OrderCode}&accountName={Uri.EscapeDataString(accountName ?? "")}";

            ViewBag.VietQrUrl = vietQrUrl;
            ViewBag.AccountNo = accountNo;
            ViewBag.AccountName = accountName;
            ViewBag.BankName = bankName;
        }

        return View(order);
    }
}

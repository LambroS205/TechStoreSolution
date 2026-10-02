using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using TechStore.Core.Entities;
using TechStore.Infrastructure.Data;
using TechStore.Web.Controllers.Api;

namespace TechStore.Web.Controllers;

public class CheckoutController : Controller
{
    private readonly TechStoreDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly TechStore.Core.Interfaces.IEmailService _emailService;
    private readonly TechStore.Core.Interfaces.ISiteSettingsService _siteSettingsService;

    public CheckoutController(
        TechStoreDbContext context,
        IConfiguration configuration,
        TechStore.Core.Interfaces.IEmailService emailService,
        TechStore.Core.Interfaces.ISiteSettingsService siteSettingsService)
    {
        _context = context;
        _configuration = configuration;
        _emailService = emailService;
        _siteSettingsService = siteSettingsService;
    }

    /// <summary>
    /// Màn hình đặt hàng & Thanh toán
    /// </summary>
    [HttpGet]
    [Route("checkout")]
    public async Task<IActionResult> Index()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
            {
                var addr = await _context.CustomerAddresses
                    .Where(a => a.UserId == userId)
                    .OrderByDescending(a => a.IsDefault)
                    .ThenByDescending(a => a.AddressId)
                    .FirstOrDefaultAsync();

                var user = await _context.Users.FindAsync(userId);
                ViewBag.DefaultAddress = addr;
                ViewBag.CustomerUser = user;
            }
        }

        return View();
    }

    /// <summary>
    /// Tiếp nhận đơn hàng từ Client (tiếp nhận mảng items, coupon và thông tin giao hàng)
    /// </summary>
    /// <summary>
    /// Bộ đếm nguyên tử đảm bảo OrderCode không bao giờ trùng lặp trong cùng một tiến trình
    /// </summary>
    private static int _orderSequence = 0;

    [HttpPost]
    [Route("checkout/place-order")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PlaceOrder([FromForm] PlaceOrderViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Vui lòng kiểm tra lại thông tin nhận hàng!";
            return RedirectToAction(nameof(Index));
        }

        // 1. Giải mã danh sách mặt hàng gửi từ LocalStorage
        List<CartItemRequest>? items;
        try
        {
            items = JsonSerializer.Deserialize<List<CartItemRequest>>(model.CartItemsJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch
        {
            TempData["ErrorMessage"] = "Giỏ hàng của bạn bị lỗi hoặc trống!";
            return RedirectToAction(nameof(Index));
        }

        if (items == null || !items.Any())
        {
            TempData["ErrorMessage"] = "Giỏ hàng trống! Vui lòng chọn sản phẩm.";
            return RedirectToAction(nameof(Index));
        }

        // ===== BỌC TOÀN BỘ QUÁ TRÌNH ĐẶT HÀNG TRONG DATABASE TRANSACTION =====
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // 2. Kiểm tra các biến thể trong CSDL để tính tổng tiền chính xác
            var variantIds = items.Select(i => i.VariantId).ToList();
            var variants = await _context.ProductVariants
                .Include(v => v.Product)
                .Where(v => variantIds.Contains(v.VariantId) && v.IsActive)
                .ToListAsync();

            decimal subTotal = 0;
            var orderDetails = new List<OrderDetail>();

            foreach (var req in items)
            {
                if (req.Quantity <= 0 || req.Quantity > 50)
                {
                    await transaction.RollbackAsync();
                    TempData["ErrorMessage"] = "Số lượng sản phẩm trong giỏ hàng không hợp lệ (từ 1 đến 50)!";
                    return RedirectToAction(nameof(Index));
                }

                var variant = variants.FirstOrDefault(v => v.VariantId == req.VariantId);
                if (variant == null)
                {
                    await transaction.RollbackAsync();
                    TempData["ErrorMessage"] = "Có sản phẩm trong giỏ hàng không còn tồn tại hoặc đã ngừng kinh doanh!";
                    return RedirectToAction(nameof(Index));
                }

                if (variant.StockQuantity < req.Quantity)
                {
                    await transaction.RollbackAsync();
                    TempData["ErrorMessage"] = $"Sản phẩm '{variant.VariantName}' chỉ còn {variant.StockQuantity} sản phẩm trong kho (bạn đặt {req.Quantity})!";
                    return RedirectToAction(nameof(Index));
                }

                decimal itemTotal = variant.SalePrice * req.Quantity;
                subTotal += itemTotal;

                orderDetails.Add(new OrderDetail
                {
                    VariantId = variant.VariantId,
                    ProductName = variant.Product.Name,
                    VariantName = variant.VariantName,
                    SKU = variant.SKU,
                    UnitPrice = variant.SalePrice,
                    Quantity = req.Quantity
                });

                // Trừ tồn kho sản phẩm
                variant.StockQuantity -= req.Quantity;
            }

            if (!orderDetails.Any())
            {
                await transaction.RollbackAsync();
                TempData["ErrorMessage"] = "Các sản phẩm trong giỏ đã hết hàng!";
                return RedirectToAction(nameof(Index));
            }

            // 3. Xử lý giảm giá (nếu có mã coupon)
            decimal discountAmount = 0;
            int? appliedCouponId = null;
            if (!string.IsNullOrWhiteSpace(model.CouponCode))
            {
                var coupon = await _context.Coupons.FirstOrDefaultAsync(c => c.Code == model.CouponCode.Trim());
                if (coupon != null && TechStore.Core.Common.CouponCalculator.TryValidate(coupon, subTotal, DateTime.UtcNow, out _))
                {
                    appliedCouponId = coupon.CouponId;
                    discountAmount = TechStore.Core.Common.CouponCalculator.CalculateDiscount(coupon, subTotal);
                    coupon.UsageCount++;
                }
            }

            decimal shippingFee = 0; // Miễn phí vận chuyển toàn quốc phong cách Best Buy
            decimal totalAmount = Math.Max(subTotal + shippingFee - discountAmount, 0);

            // 4. Lấy UserId nếu khách đã đăng nhập
            int? userId = null;
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int uid))
            {
                userId = uid;
            }

            // 5. Tạo đơn hàng (Order) — Dùng Interlocked.Increment + Timestamp đảm bảo OrderCode duy nhất
            int seq = Interlocked.Increment(ref _orderSequence) % 10000;
            string orderCode = $"ORD{DateTime.UtcNow:yyyyMMddHHmmss}-{seq:D4}";
            var order = new Order
            {
                OrderCode = orderCode,
                UserId = userId,
                CustomerName = model.CustomerName,
                CustomerPhone = model.CustomerPhone,
                CustomerEmail = model.CustomerEmail,
                ShippingAddress = $"{model.AddressDetail}, {model.Ward}, {model.District}, {model.Province}",
                OrderNotes = model.OrderNotes,
                CouponId = appliedCouponId,
                DiscountAmount = discountAmount,
                SubTotal = subTotal,
                ShippingFee = shippingFee,
                TotalAmount = totalAmount,
                PaymentMethod = model.PaymentMethod == "VietQR" ? "VietQR" : "COD",
                PaymentStatus = "Pending",
                OrderStatus = "Pending",
                CreatedAt = DateTime.UtcNow,
                OrderDetails = orderDetails
            };

            await _context.Orders.AddAsync(order);
            await _context.SaveChangesAsync();

            // 5.1 Ghi nhận lịch sử khởi tạo đơn hàng
            await _context.OrderStatusHistories.AddAsync(new OrderStatusHistory
            {
                OrderId = order.OrderId,
                PreviousStatus = null,
                NewStatus = "Pending",
                Note = "Khách hàng hoàn tất đặt hàng trên hệ thống TechStore",
                ChangedBy = userId,
                ChangedAt = DateTime.UtcNow
            });

            // 6. Ghi nhận lịch sử biến động xuất kho bán hàng
            int createdById = userId ?? 1;
            foreach (var detail in order.OrderDetails)
            {
                await _context.InventoryTransactions.AddAsync(new InventoryTransaction
                {
                    VariantId = detail.VariantId,
                    TransactionType = "EXPORT_ORDER",
                    Quantity = detail.Quantity,
                    UnitPrice = detail.UnitPrice,
                    ReferenceCode = order.OrderCode,
                    Note = $"Xuất kho bán hàng theo đơn #{order.OrderCode}",
                    CreatedBy = createdById,
                    CreatedAt = DateTime.UtcNow
                });
            }

            // Lưu tất cả trong một lần duy nhất và commit transaction
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            // 7. Gửi email xác nhận đặt hàng kèm hóa đơn & mã VietQR (ngoài transaction vì không rollback được email)
            await _emailService.SendOrderConfirmationEmailAsync(order);

            return RedirectToAction(nameof(Success), new { orderCode = order.OrderCode });
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            TempData["ErrorMessage"] = "Đã xảy ra lỗi trong quá trình xử lý đơn hàng. Vui lòng thử lại!";
            return RedirectToAction(nameof(Index));
        }
    }

    /// <summary>
    /// Trang hiển thị kết quả đặt hàng thành công & Sinh mã thanh toán động VietQR NAPAS 247
    /// </summary>
    [HttpGet]
    [Route("checkout/success/{orderCode}")]
    public async Task<IActionResult> Success(string orderCode)
    {
        var order = await _context.Orders
            .Include(o => o.OrderDetails)
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.OrderCode == orderCode);

        if (order == null)
        {
            return NotFound();
        }

        // Lấy thông tin tài khoản ngân hàng từ SiteSettings CMS (ưu tiên) hoặc appsettings.json
        var bankId = await _siteSettingsService.GetValueAsync("VietQr.BankId", _configuration["VietQrSettings:BankId"] ?? "970422");
        var accountNo = await _siteSettingsService.GetValueAsync("VietQr.AccountNo", _configuration["VietQrSettings:AccountNo"] ?? "0869162534");
        var accountName = await _siteSettingsService.GetValueAsync("VietQr.AccountName", _configuration["VietQrSettings:AccountName"] ?? "CONG TY TNHH TECHSTORE VIET NAM");
        var template = await _siteSettingsService.GetValueAsync("VietQr.Template", _configuration["VietQrSettings:Template"] ?? "compact2");
        var bankName = await _siteSettingsService.GetValueAsync("VietQr.BankName", _configuration["VietQrSettings:BankName"] ?? "MBBank (Ngân hàng Quân Đội)");

        // URL sinh mã QR thanh toán động VietQR chuẩn NAPAS 247
        string vietQrUrl = $"https://img.vietqr.io/image/{bankId}-{accountNo}-{template}.png?amount={(long)order.TotalAmount}&addInfo={order.OrderCode}&accountName={Uri.EscapeDataString(accountName ?? "")}";

        ViewBag.VietQrUrl = vietQrUrl;
        ViewBag.AccountNo = accountNo;
        ViewBag.AccountName = accountName;
        ViewBag.BankName = bankName;

        return View(order);
    }
}

public class PlaceOrderViewModel
{
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string? CustomerEmail { get; set; }
    public string Province { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public string Ward { get; set; } = string.Empty;
    public string AddressDetail { get; set; } = string.Empty;
    public string? OrderNotes { get; set; }
    public string PaymentMethod { get; set; } = "COD"; // "COD" hoặc "VietQR"
    public string? CouponCode { get; set; }
    public string CartItemsJson { get; set; } = "[]";
}
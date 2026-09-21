using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechStore.Core.Entities;
using TechStore.Core.Interfaces;
using TechStore.Infrastructure.Data;
using TechStore.Web.Security;

namespace TechStore.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
[HasPermission("Orders.View")]
public class OrderController : Controller
{
    private readonly TechStoreDbContext _context;
    private readonly IAuditLogService _auditLogService;

    public OrderController(TechStoreDbContext context, IAuditLogService auditLogService)
    {
        _context = context;
        _auditLogService = auditLogService;
    }

    /// <summary>
    /// Hiển thị danh sách Đơn hàng & Bộ đếm thanh toán VietQR
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Index(string? status, string? paymentStatus, string? search)
    {
        var query = _context.Orders
            .Include(o => o.OrderDetails)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(o => o.OrderStatus == status);

        if (!string.IsNullOrWhiteSpace(paymentStatus))
            query = query.Where(o => o.PaymentStatus == paymentStatus);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(o => o.OrderCode.Contains(search) || o.CustomerPhone.Contains(search) || o.CustomerName.Contains(search));

        var orders = await query.OrderByDescending(o => o.CreatedAt).ToListAsync();

        // Đếm số lượng đơn VietQR đang chờ nhân viên kiểm tra đối soát
        ViewBag.PendingVietQrCount = await _context.Orders
            .CountAsync(o => o.PaymentMethod == "VietQR" && o.PaymentStatus == "Pending");

        ViewBag.CurrentStatus = status ?? "All";
        return View(orders);
    }

    /// <summary>
    /// Duyệt và xác nhận tiền đã về qua cổng VietQR
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission("Orders.ApprovePayment")]
    public async Task<IActionResult> VerifyVietQrPayment(int orderId, string transactionRef)
    {
        var order = await _context.Orders.FindAsync(orderId);
        if (order == null) return NotFound();

        order.PaymentStatus = "Paid";
        order.OrderStatus = "Processing"; // Chuyển sang đang đóng gói xử lý
        order.UpdatedAt = DateTime.UtcNow;

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        int.TryParse(userIdClaim?.Value, out int adminId);

        // Tạo bản ghi nhật ký giao dịch đối soát
        var transaction = new PaymentTransaction
        {
            OrderId = order.OrderId,
            PaymentMethod = "VietQR",
            TransactionReference = string.IsNullOrWhiteSpace(transactionRef) ? $"VQR-APP-{DateTime.UtcNow.Ticks}" : transactionRef,
            Amount = order.TotalAmount,
            Status = "Success",
            BankCode = "MBBank",
            ConfirmedBy = adminId > 0 ? adminId : null,
            ConfirmedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        await _context.PaymentTransactions.AddAsync(transaction);
        await _context.SaveChangesAsync();

        // Ghi nhận nhật ký kiểm toán cho hành động duyệt tiền VietQR
        await _auditLogService.LogAsync(
            action: "VerifyVietQrPayment",
            module: "Orders",
            recordId: order.OrderCode,
            oldValues: new { PaymentStatus = "Pending", OrderStatus = "Pending" },
            newValues: new { PaymentStatus = "Paid", OrderStatus = "Processing", TransactionReference = transaction.TransactionReference, Amount = order.TotalAmount }
        );

        TempData["SuccessMessage"] = $"Đã xác nhận thanh toán thành công cho đơn hàng {order.OrderCode}!";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Cập nhật trạng thái tiến trình đơn hàng (Xác nhận, Đang giao, Đã giao)
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission("Orders.Edit")]
    public async Task<IActionResult> UpdateStatus(int orderId, string newStatus)
    {
        var order = await _context.Orders.FindAsync(orderId);
        if (order == null) return NotFound();

        order.OrderStatus = newStatus;
        if (newStatus == "Delivered" && order.PaymentMethod == "COD")
        {
            order.PaymentStatus = "Paid"; // Thu tiền COD thành công khi giao hàng
        }
        order.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = $"Đã cập nhật trạng thái đơn {order.OrderCode} thành '{newStatus}'.";
        return RedirectToAction(nameof(Index));
    }
}
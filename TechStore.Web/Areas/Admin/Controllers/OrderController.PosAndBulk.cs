using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechStore.Core.Entities;
using TechStore.Web.Security;

namespace TechStore.Web.Areas.Admin.Controllers;

public partial class OrderController
{
    /// <summary>
    /// Giao diện POS bán hàng trực tiếp tại quầy showroom
    /// </summary>
    [HttpGet]
    [HasPermission("Orders.Create")]
    public IActionResult CreatePos()
    {
        return View();
    }

    /// <summary>
    /// Tìm kiếm nhanh sản phẩm & biến thể SKU cho nhân viên tại quầy POS
    /// </summary>
    [HttpGet]
    [HasPermission("Orders.Create")]
    public async Task<IActionResult> SearchVariantsForPos(string? term)
    {
        if (string.IsNullOrWhiteSpace(term))
            return Json(new List<object>());

        term = term.Trim().ToLower();
        var variants = await _context.ProductVariants
            .Include(v => v.Product)
            .AsNoTracking()
            .Where(v => v.IsActive && v.Product.IsActive && (
                v.SKU.ToLower().Contains(term) ||
                v.VariantName.ToLower().Contains(term) ||
                v.Product.Name.ToLower().Contains(term)))
            .OrderByDescending(v => v.StockQuantity > 0)
            .ThenBy(v => v.Product.Name)
            .Take(15)
            .Select(v => new
            {
                variantId = v.VariantId,
                productId = v.ProductId,
                productName = v.Product.Name,
                variantName = v.VariantName,
                sku = v.SKU,
                price = v.SalePrice,
                originalPrice = v.OriginalPrice,
                stock = v.StockQuantity,
                image = !string.IsNullOrEmpty(v.Product.FeaturedImage) ? v.Product.FeaturedImage : "/assets/images/placeholder.png"
            })
            .ToListAsync();

        return Json(variants);
    }

    /// <summary>
    /// Xử lý tạo đơn hàng POS tại quầy, xuất kho ngay lập tức và in hóa đơn
    /// </summary>
    [HttpPost]
    [HasPermission("Orders.Create")]
    public async Task<IActionResult> CreatePos([FromBody] PosOrderDto model)
    {
        if (model == null || model.Items == null || !model.Items.Any())
        {
            return BadRequest(new { success = false, message = "Đơn hàng phải có ít nhất một sản phẩm!" });
        }

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        int.TryParse(userIdClaim?.Value, out int adminId);

        var variantIds = model.Items.Select(i => i.VariantId).Distinct().ToList();
        var variants = await _context.ProductVariants
            .Include(v => v.Product)
            .Where(v => variantIds.Contains(v.VariantId))
            .ToListAsync();

        foreach (var item in model.Items)
        {
            var variant = variants.FirstOrDefault(v => v.VariantId == item.VariantId);
            if (variant == null)
            {
                return BadRequest(new { success = false, message = $"Sản phẩm ID {item.VariantId} không tồn tại!" });
            }
            if (variant.StockQuantity < item.Quantity)
            {
                return BadRequest(new { success = false, message = $"Mặt hàng '{variant.Product.Name} - {variant.VariantName}' chỉ còn {variant.StockQuantity} trong kho (yêu cầu {item.Quantity})!" });
            }
        }

        string orderCode = $"POS-{DateTime.UtcNow:yyMMdd}-{new Random().Next(1000, 9999)}";

        decimal subTotal = 0;
        var orderDetails = new List<OrderDetail>();

        foreach (var item in model.Items)
        {
            var variant = variants.First(v => v.VariantId == item.VariantId);
            decimal itemPrice = variant.SalePrice;
            decimal lineTotal = itemPrice * item.Quantity;
            subTotal += lineTotal;

            variant.StockQuantity -= item.Quantity;

            var tx = new InventoryTransaction
            {
                VariantId = variant.VariantId,
                TransactionType = "EXPORT_SALE",
                Quantity = item.Quantity,
                UnitPrice = itemPrice,
                ReferenceCode = orderCode,
                Note = $"Xuất bán tại quầy cho khách {model.CustomerName} ({orderCode})",
                CreatedBy = adminId > 0 ? adminId : 1,
                CreatedAt = DateTime.UtcNow
            };
            await _context.InventoryTransactions.AddAsync(tx);

            orderDetails.Add(new OrderDetail
            {
                VariantId = variant.VariantId,
                ProductName = variant.Product.Name,
                VariantName = variant.VariantName,
                SKU = variant.SKU,
                Quantity = item.Quantity,
                UnitPrice = itemPrice
            });
        }

        decimal discount = Math.Max(0, model.DiscountAmount);
        decimal finalTotal = Math.Max(0, subTotal - discount);

        var order = new Order
        {
            OrderCode = orderCode,
            CustomerName = string.IsNullOrWhiteSpace(model.CustomerName) ? "Khách Lẻ Mua Tại Quầy" : model.CustomerName.Trim(),
            CustomerPhone = string.IsNullOrWhiteSpace(model.CustomerPhone) ? "0900000000" : model.CustomerPhone.Trim(),
            CustomerEmail = model.CustomerEmail?.Trim(),
            ShippingAddress = "Giao trực tiếp tại quầy TechStore Showroom",
            PaymentMethod = model.PaymentMethod,
            PaymentStatus = "Paid",
            OrderStatus = "Delivered",
            SubTotal = subTotal,
            DiscountAmount = discount,
            ShippingFee = 0,
            TotalAmount = finalTotal,
            OrderNotes = string.IsNullOrWhiteSpace(model.Notes) ? "[POS Showroom]" : $"[POS Showroom] {model.Notes.Trim()}",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            OrderDetails = orderDetails
        };

        await _context.Orders.AddAsync(order);

        var history = new OrderStatusHistory
        {
            OrderId = order.OrderId,
            PreviousStatus = null,
            NewStatus = "Delivered",
            Note = $"Đơn hàng hoàn tất thanh toán tại quầy bằng phương thức {model.PaymentMethod}",
            ChangedBy = adminId > 0 ? adminId : null,
            ChangedAt = DateTime.UtcNow
        };
        await _context.OrderStatusHistories.AddAsync(history);

        await _context.SaveChangesAsync();

        await _auditLogService.LogAsync(
            action: "CreatePosOrder",
            module: "Orders",
            recordId: order.OrderCode,
            oldValues: null,
            newValues: new { TotalAmount = finalTotal, PaymentMethod = model.PaymentMethod, ItemsCount = model.Items.Count }
        );

        decimal change = model.CashGiven > finalTotal ? model.CashGiven - finalTotal : 0;

        return Json(new
        {
            success = true,
            orderId = order.OrderId,
            orderCode = order.OrderCode,
            totalAmount = finalTotal,
            changeAmount = change,
            message = "Tạo đơn hàng tại quầy thành công!"
        });
    }

    /// <summary>
    /// Thao tác cập nhật trạng thái đơn hàng hàng loạt (Bulk Status Update)
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission("Orders.Edit")]
    [HttpGet]
    public IActionResult BulkUpdateStatus() => RedirectToAction(nameof(Index));

    public async Task<IActionResult> BulkUpdateStatus(List<int> orderIds, string newStatus, string? returnUrl)
    {
        if (orderIds == null || !orderIds.Any() || string.IsNullOrWhiteSpace(newStatus))
        {
            TempData["ErrorMessage"] = "Vui lòng chọn ít nhất một đơn hàng và trạng thái cần cập nhật!";
            return Redirect(returnUrl ?? "/Admin/Order");
        }

        var validStatuses = new[] { "Pending", "Confirmed", "Processing", "Shipping", "Delivered", "Cancelled" };
        if (!validStatuses.Contains(newStatus))
        {
            TempData["ErrorMessage"] = "Trạng thái đơn hàng không hợp lệ!";
            return Redirect(returnUrl ?? "/Admin/Order");
        }

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        int.TryParse(userIdClaim?.Value, out int adminId);

        var orders = await _context.Orders
            .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Variant)
            .Where(o => orderIds.Contains(o.OrderId))
            .ToListAsync();

        int updatedCount = 0;
        foreach (var order in orders)
        {
            string oldStatus = order.OrderStatus;
            if (oldStatus == newStatus) continue;

            if (newStatus == "Cancelled" && oldStatus != "Cancelled")
            {
                foreach (var detail in order.OrderDetails)
                {
                    if (detail.Variant != null)
                    {
                        detail.Variant.StockQuantity += detail.Quantity;
                        var tx = new InventoryTransaction
                        {
                            VariantId = detail.VariantId,
                            TransactionType = "IMPORT_RETURN",
                            Quantity = detail.Quantity,
                            UnitPrice = detail.UnitPrice,
                            ReferenceCode = order.OrderCode,
                            Note = $"Hoàn tồn kho tự động do hủy đơn hàng {order.OrderCode} (Thao tác hàng loạt)",
                            CreatedBy = adminId > 0 ? adminId : 1,
                            CreatedAt = DateTime.UtcNow
                        };
                        await _context.InventoryTransactions.AddAsync(tx);
                    }
                }
            }

            if (newStatus == "Delivered" && order.PaymentMethod == "COD")
            {
                order.PaymentStatus = "Paid";
            }

            order.OrderStatus = newStatus;
            order.UpdatedAt = DateTime.UtcNow;

            var history = new OrderStatusHistory
            {
                OrderId = order.OrderId,
                PreviousStatus = oldStatus,
                NewStatus = newStatus,
                Note = $"Nhân viên cập nhật trạng thái hàng loạt thành '{newStatus}'",
                ChangedBy = adminId > 0 ? adminId : null,
                ChangedAt = DateTime.UtcNow
            };
            await _context.OrderStatusHistories.AddAsync(history);
            updatedCount++;
        }

        await _context.SaveChangesAsync();

        await _auditLogService.LogAsync(
            action: "BulkUpdateOrderStatus",
            module: "Orders",
            recordId: $"Batch({updatedCount})",
            oldValues: null,
            newValues: new { NewStatus = newStatus, Count = updatedCount }
        );

        TempData["SuccessMessage"] = $"Đã cập nhật trạng thái thành công cho {updatedCount} đơn hàng thành '{newStatus}'!";
        return Redirect(returnUrl ?? "/Admin/Order");
    }

    /// <summary>
    /// In phiếu giao hàng / hóa đơn hàng loạt cho nhiều đơn đã chọn
    /// </summary>
    [HttpGet]
    [HasPermission("Orders.View")]
    public async Task<IActionResult> BulkPrintInvoices(string ids)
    {
        if (string.IsNullOrWhiteSpace(ids))
        {
            return BadRequest("Vui lòng truyền danh sách ID đơn hàng cần in!");
        }

        var orderIdList = ids.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => int.TryParse(s.Trim(), out int id) ? id : 0)
            .Where(id => id > 0)
            .ToList();

        var orders = await _context.Orders
            .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Variant)
                    .ThenInclude(v => v.Product)
            .Include(o => o.User)
            .Where(o => orderIdList.Contains(o.OrderId))
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        return View(orders);
    }
}

public class PosOrderDto
{
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string? CustomerEmail { get; set; }
    public string PaymentMethod { get; set; } = "Cash";
    public decimal CashGiven { get; set; }
    public decimal DiscountAmount { get; set; }
    public string? Notes { get; set; }
    public List<PosOrderItemDto> Items { get; set; } = new();
}

public class PosOrderItemDto
{
    public int VariantId { get; set; }
    public int Quantity { get; set; }
}

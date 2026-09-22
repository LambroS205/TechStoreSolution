using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using TechStore.Core.Entities;
using TechStore.Core.Interfaces;
using TechStore.Infrastructure.Data;
using TechStore.Web.Security;

namespace TechStore.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
public class InventoryController : Controller
{
    private readonly TechStoreDbContext _context;
    private readonly IAuditLogService _auditLogService;

    public InventoryController(TechStoreDbContext context, IAuditLogService auditLogService)
    {
        _context = context;
        _auditLogService = auditLogService;
    }

    /// <summary>
    /// Báo cáo tổng quan tồn kho các biến thể sản phẩm & cảnh báo thiếu hàng
    /// </summary>
    [HttpGet]
    [HasPermission("Inventory.View")]
    public async Task<IActionResult> Index(string? q = null, string? status = "All", int page = 1)
    {
        int pageSize = 15;
        var query = _context.ProductVariants
            .Include(v => v.Product)
                .ThenInclude(p => p.Category)
            .Include(v => v.Product)
                .ThenInclude(p => p.Brand)
            .AsNoTracking()
            .AsQueryable();

        // 1. Tìm kiếm theo tên sản phẩm, biến thể hoặc SKU
        if (!string.IsNullOrWhiteSpace(q))
        {
            var keyword = q.Trim().ToLower();
            query = query.Where(v => v.Product.Name.ToLower().Contains(keyword) ||
                                     v.VariantName.ToLower().Contains(keyword) ||
                                     v.SKU.ToLower().Contains(keyword));
        }

        // 2. Lọc theo trạng thái tồn kho
        if (status == "OutOfStock")
        {
            query = query.Where(v => v.StockQuantity <= 0);
        }
        else if (status == "LowStock")
        {
            query = query.Where(v => v.StockQuantity > 0 && v.StockQuantity <= 10);
        }
        else if (status == "InStock")
        {
            query = query.Where(v => v.StockQuantity > 10);
        }

        int totalItems = await query.CountAsync();
        var variants = await query
            .OrderBy(v => v.StockQuantity) // Ưu tiên hiển thị tồn kho thấp lên đầu
            .ThenBy(v => v.Product.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // Thống kê toàn hệ thống
        var allVariants = await _context.ProductVariants.AsNoTracking().ToListAsync();
        ViewBag.TotalQuantity = allVariants.Sum(v => v.StockQuantity);
        ViewBag.TotalValue = allVariants.Sum(v => v.StockQuantity * v.SalePrice);
        ViewBag.LowStockCount = allVariants.Count(v => v.StockQuantity > 0 && v.StockQuantity <= 10);
        ViewBag.OutOfStockCount = allVariants.Count(v => v.StockQuantity <= 0);

        ViewBag.SearchQuery = q;
        ViewBag.Status = status;
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
        ViewBag.TotalItems = totalItems;

        return View(variants);
    }

    /// <summary>
    /// Màn hình lập phiếu nhập kho từ nhà cung cấp
    /// </summary>
    [HttpGet]
    [HasPermission("Inventory.Import")]
    public async Task<IActionResult> Import(int? variantId = null)
    {
        await LoadViewBagsAsync();

        var model = new ImportInventoryViewModel();
        if (variantId.HasValue)
        {
            var v = await _context.ProductVariants.Include(pv => pv.Product).FirstOrDefaultAsync(pv => pv.VariantId == variantId.Value);
            if (v != null)
            {
                model.VariantId = v.VariantId;
                model.UnitPrice = v.OriginalPrice > 0 ? v.OriginalPrice : v.SalePrice * 0.8m;
                ViewBag.SelectedVariantName = $"{v.Product.Name} - {v.VariantName} (SKU: {v.SKU}) [Tồn hiện tại: {v.StockQuantity}]";
            }
        }

        return View(model);
    }

    /// <summary>
    /// Xử lý lập phiếu nhập kho và cộng dồn số lượng vào biến thể
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission("Inventory.Import")]
    public async Task<IActionResult> Import(ImportInventoryViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await LoadViewBagsAsync();
            return View(model);
        }

        var variant = await _context.ProductVariants
            .Include(v => v.Product)
            .FirstOrDefaultAsync(v => v.VariantId == model.VariantId);

        if (variant == null)
        {
            ModelState.AddModelError("VariantId", "Không tìm thấy biến thể sản phẩm được chọn!");
            await LoadViewBagsAsync();
            return View(model);
        }

        int? currentUserId = null;
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int uid)) currentUserId = uid;

        int oldStock = variant.StockQuantity;
        variant.StockQuantity += model.Quantity;

        string refCode = string.IsNullOrWhiteSpace(model.ReferenceCode)
            ? $"IMP{DateTime.UtcNow:yyyyMMdd}-{new Random().Next(1000, 9999)}"
            : model.ReferenceCode.Trim().ToUpper();

        string note = model.Note?.Trim() ?? $"Nhập {model.Quantity} sản phẩm vào kho";
        if (model.SupplierId.HasValue && model.SupplierId.Value > 0)
        {
            var sup = await _context.Suppliers.FindAsync(model.SupplierId.Value);
            if (sup != null)
            {
                note = $"[NCC: {sup.Name}] " + note;
            }
        }

        var tx = new InventoryTransaction
        {
            VariantId = variant.VariantId,
            TransactionType = "IMPORT",
            Quantity = model.Quantity,
            UnitPrice = model.UnitPrice,
            ReferenceCode = refCode,
            Note = note,
            CreatedBy = currentUserId ?? 1,
            CreatedAt = DateTime.UtcNow
        };

        await _context.InventoryTransactions.AddAsync(tx);
        await _context.SaveChangesAsync();

        await _auditLogService.LogAsync(
            action: "IMPORT_INVENTORY",
            module: "Inventory",
            recordId: tx.InventoryTxId.ToString(),
            oldValues: new { StockQuantity = oldStock },
            newValues: new { StockQuantity = variant.StockQuantity, Added = model.Quantity, ReferenceCode = refCode, SupplierId = model.SupplierId }
        );

        TempData["SuccessMessage"] = $"Lập phiếu nhập kho thành công! Đã cộng thêm {model.Quantity} sản phẩm vào SKU {variant.SKU}.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Sổ nhật ký chi tiết các giao dịch xuất - nhập - điều chỉnh kho
    /// </summary>
    [HttpGet]
    [HasPermission("Inventory.View")]
    public async Task<IActionResult> Transactions(string? type = "All", int page = 1)
    {
        int pageSize = 20;
        var query = _context.InventoryTransactions
            .Include(t => t.Variant)
                .ThenInclude(v => v.Product)
            .Include(t => t.User)
            .AsNoTracking()
            .AsQueryable();

        if (type == "IMPORT")
        {
            query = query.Where(t => t.TransactionType == "IMPORT");
        }
        else if (type == "EXPORT_ORDER")
        {
            query = query.Where(t => t.TransactionType == "EXPORT_ORDER");
        }

        int totalItems = await query.CountAsync();
        var transactions = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.Type = type;
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
        ViewBag.TotalItems = totalItems;

        return View(transactions);
    }

    private async Task LoadViewBagsAsync()
    {
        var variants = await _context.ProductVariants
            .Include(v => v.Product)
            .Where(v => v.IsActive && v.Product.IsActive)
            .OrderBy(v => v.Product.Name)
            .ThenBy(v => v.VariantName)
            .Select(v => new
            {
                v.VariantId,
                DisplayName = $"{v.Product.Name} - {v.VariantName} (SKU: {v.SKU}) [Tồn: {v.StockQuantity}]"
            })
            .ToListAsync();

        ViewBag.Variants = new SelectList(variants, "VariantId", "DisplayName");

        var suppliers = await _context.Suppliers
            .Where(s => s.IsActive)
            .OrderBy(s => s.Name)
            .ToListAsync();
        ViewBag.Suppliers = new SelectList(suppliers, "SupplierId", "Name");
    }
}

public class ImportInventoryViewModel
{
    [Required(ErrorMessage = "Vui lòng chọn sản phẩm / biến thể cần nhập kho!")]
    public int VariantId { get; set; }

    public int? SupplierId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập số lượng nhập kho!")]
    [Range(1, 100000, ErrorMessage = "Số lượng nhập kho phải lớn hơn 0!")]
    public int Quantity { get; set; } = 10;

    [Required(ErrorMessage = "Vui lòng nhập giá nhập đơn vị!")]
    [Range(0, 1000000000, ErrorMessage = "Giá nhập đơn vị không hợp lệ!")]
    public decimal UnitPrice { get; set; }

    public string? ReferenceCode { get; set; }

    public string? Note { get; set; }
}

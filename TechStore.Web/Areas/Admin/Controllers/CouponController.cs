using System;
using System.Linq;
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
[HasPermission("Coupons.Manage")]
public class CouponController : Controller
{
    private readonly TechStoreDbContext _context;
    private readonly IAuditLogService _auditLogService;

    public CouponController(TechStoreDbContext context, IAuditLogService auditLogService)
    {
        _context = context;
        _auditLogService = auditLogService;
    }

    /// <summary>
    /// Danh sách mã giảm giá khuyến mãi
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Index(string? search, string? status)
    {
        var query = _context.Coupons.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToUpperInvariant();
            query = query.Where(c => c.Code.ToUpper().Contains(s) || c.Description.Contains(search.Trim()));
        }

        var now = DateTime.UtcNow;
        if (status == "Active")
        {
            query = query.Where(c => c.IsActive && c.StartDate <= now && c.EndDate >= now && c.UsageCount < c.UsageLimit);
        }
        else if (status == "Expired")
        {
            query = query.Where(c => !c.IsActive || c.EndDate < now || c.UsageCount >= c.UsageLimit);
        }

        var coupons = await query
            .OrderByDescending(c => c.CouponId)
            .ToListAsync();

        ViewBag.Search = search;
        ViewBag.Status = status ?? "All";

        return View(coupons);
    }

    /// <summary>
    /// Thêm mới hoặc cập nhật mã giảm giá
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(Coupon coupon)
    {
        if (string.IsNullOrWhiteSpace(coupon.Code))
        {
            TempData["ErrorMessage"] = "Mã Voucher không được để trống!";
            return RedirectToAction(nameof(Index));
        }

        string code = coupon.Code.Trim().ToUpperInvariant();

        if (coupon.DiscountValue <= 0)
        {
            TempData["ErrorMessage"] = "Giá trị giảm giá phải lớn hơn 0!";
            return RedirectToAction(nameof(Index));
        }

        if (coupon.DiscountType == "Percentage" && coupon.DiscountValue > 100)
        {
            TempData["ErrorMessage"] = "Giảm giá theo phần trăm không được vượt quá 100%!";
            return RedirectToAction(nameof(Index));
        }

        if (coupon.EndDate < coupon.StartDate)
        {
            TempData["ErrorMessage"] = "Ngày kết thúc phải diễn ra sau ngày bắt đầu!";
            return RedirectToAction(nameof(Index));
        }

        // Kiểm tra trùng Code ngoại trừ chính nó
        bool isDuplicateCode = await _context.Coupons
            .AnyAsync(c => c.Code == code && c.CouponId != coupon.CouponId);

        if (isDuplicateCode)
        {
            TempData["ErrorMessage"] = $"Mã giảm giá '{code}' đã tồn tại!";
            return RedirectToAction(nameof(Index));
        }

        if (coupon.CouponId == 0)
        {
            coupon.Code = code;
            coupon.Description = coupon.Description?.Trim() ?? string.Empty;
            coupon.UsageCount = 0;

            await _context.Coupons.AddAsync(coupon);
            await _context.SaveChangesAsync();

            await _auditLogService.LogAsync(
                action: "CreateCoupon",
                module: "Coupons",
                recordId: coupon.CouponId.ToString(),
                oldValues: null,
                newValues: new { coupon.Code, coupon.DiscountType, coupon.DiscountValue, coupon.UsageLimit }
            );

            TempData["SuccessMessage"] = $"Đã tạo mã giảm giá '{coupon.Code}' thành công!";
        }
        else
        {
            var existing = await _context.Coupons.FindAsync(coupon.CouponId);
            if (existing == null) return NotFound();

            var oldValues = new { existing.Code, existing.DiscountType, existing.DiscountValue, existing.UsageLimit, existing.IsActive };

            existing.Code = code;
            existing.Description = coupon.Description?.Trim() ?? string.Empty;
            existing.DiscountType = coupon.DiscountType;
            existing.DiscountValue = coupon.DiscountValue;
            existing.MinOrderAmount = coupon.MinOrderAmount;
            existing.MaxDiscountAmount = coupon.MaxDiscountAmount;
            existing.UsageLimit = coupon.UsageLimit;
            existing.StartDate = coupon.StartDate;
            existing.EndDate = coupon.EndDate;
            existing.IsActive = coupon.IsActive;

            await _context.SaveChangesAsync();

            await _auditLogService.LogAsync(
                action: "UpdateCoupon",
                module: "Coupons",
                recordId: existing.CouponId.ToString(),
                oldValues: oldValues,
                newValues: new { existing.Code, existing.DiscountType, existing.DiscountValue, existing.UsageLimit, existing.IsActive }
            );

            TempData["SuccessMessage"] = $"Đã cập nhật mã giảm giá '{existing.Code}' thành công!";
        }

        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Bật/Tắt trạng thái hoạt động qua AJAX
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        var coupon = await _context.Coupons.FindAsync(id);
        if (coupon == null) return Json(new { success = false, message = "Không tìm thấy mã giảm giá." });

        coupon.IsActive = !coupon.IsActive;
        await _context.SaveChangesAsync();

        return Json(new { success = true, isActive = coupon.IsActive });
    }

    /// <summary>
    /// Xóa mã giảm giá
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var coupon = await _context.Coupons.FindAsync(id);
        if (coupon == null)
        {
            TempData["ErrorMessage"] = "Mã giảm giá không tồn tại.";
            return RedirectToAction(nameof(Index));
        }

        // Kiểm tra xem đã có đơn hàng sử dụng mã này chưa
        if (coupon.UsageCount > 0)
        {
            coupon.IsActive = false;
            await _context.SaveChangesAsync();
            TempData["ErrorMessage"] = $"Mã giảm giá '{coupon.Code}' đã có {coupon.UsageCount} lượt áp dụng, hệ thống đã tự động chuyển sang trạng thái Vô hiệu hóa để đảm bảo toàn vẹn dữ liệu đơn hàng.";
        }
        else
        {
            _context.Coupons.Remove(coupon);
            await _context.SaveChangesAsync();

            await _auditLogService.LogAsync(
                action: "DeleteCoupon",
                module: "Coupons",
                recordId: id.ToString(),
                oldValues: new { coupon.Code },
                newValues: null
            );

            TempData["SuccessMessage"] = $"Đã xóa mã giảm giá '{coupon.Code}' thành công!";
        }

        return RedirectToAction(nameof(Index));
    }
}

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechStore.Core.Entities;
using TechStore.Core.Interfaces;
using TechStore.Infrastructure.Data;
using TechStore.Web.Security;

namespace TechStore.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
[HasPermission("Banners.Manage")]
public class BannerController : Controller
{
    private readonly TechStoreDbContext _context;
    private readonly IFileStorageService _fileStorageService;
    private readonly ICacheInvalidationService _cacheInvalidation;

    public BannerController(TechStoreDbContext context, IFileStorageService fileStorageService, ICacheInvalidationService cacheInvalidation)
    {
        _context = context;
        _fileStorageService = fileStorageService;
        _cacheInvalidation = cacheInvalidation;
    }

    /// <summary>
    /// Hiển thị danh sách Banner quảng cáo đa vị trí
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Index(string? position)
    {
        var query = _context.Banners.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(position))
        {
            query = query.Where(b => b.Position == position);
        }

        var banners = await query
            .OrderBy(b => b.Position)
            .ThenBy(b => b.DisplayOrder)
            .ToListAsync();

        ViewBag.CurrentPosition = position ?? "All";
        return View(banners);
    }

    [HttpGet]
    public IActionResult Save() => RedirectToAction(nameof(Index));

    [HttpGet]
    public IActionResult ToggleStatus() => RedirectToAction(nameof(Index));

    [HttpGet]
    public IActionResult Delete() => RedirectToAction(nameof(Index));

    /// <summary>
    /// Thêm mới hoặc cập nhật Banner kèm upload file ảnh
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(Banner banner, IFormFile? imageFile, IFormFile? mobileImageFile)
    {
        if (imageFile != null && imageFile.Length > 0)
        {
            using var stream = imageFile.OpenReadStream();
            banner.ImageUrl = await _fileStorageService.SaveFileAsync(stream, imageFile.FileName, "banners");
        }

        if (mobileImageFile != null && mobileImageFile.Length > 0)
        {
            using var stream = mobileImageFile.OpenReadStream();
            banner.MobileImageUrl = await _fileStorageService.SaveFileAsync(stream, mobileImageFile.FileName, "banners");
        }

        if (string.IsNullOrWhiteSpace(banner.Title) || string.IsNullOrWhiteSpace(banner.ImageUrl))
        {
            TempData["ErrorMessage"] = "Tiêu đề và đường dẫn ảnh Banner không được để trống!";
            return RedirectToAction(nameof(Index));
        }

        if (banner.BannerId == 0)
        {
            banner.CreatedDate = DateTime.UtcNow;
            await _context.Banners.AddAsync(banner);
            TempData["SuccessMessage"] = "Đã thêm mới Banner thành công!";
        }
        else
        {
            var existing = await _context.Banners.FindAsync(banner.BannerId);
            if (existing == null) return NotFound();

            existing.Title = banner.Title;
            existing.Subtitle = banner.Subtitle;
            existing.ImageUrl = banner.ImageUrl;
            if (!string.IsNullOrWhiteSpace(banner.MobileImageUrl))
                existing.MobileImageUrl = banner.MobileImageUrl;
            existing.TargetUrl = banner.TargetUrl;
            existing.Position = banner.Position;
            existing.DisplayOrder = banner.DisplayOrder;
            existing.StartDate = banner.StartDate;
            existing.EndDate = banner.EndDate;
            existing.IsActive = banner.IsActive;

            TempData["SuccessMessage"] = "Đã cập nhật thông tin Banner thành công!";
        }

        await _context.SaveChangesAsync();
        _cacheInvalidation.InvalidateHomePageCache();
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Bật/Tắt trạng thái hiển thị của Banner qua AJAX
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        var banner = await _context.Banners.FindAsync(id);
        if (banner == null) return Json(new { success = false, message = "Không tìm thấy Banner." });

        banner.IsActive = !banner.IsActive;
        await _context.SaveChangesAsync();
        _cacheInvalidation.InvalidateHomePageCache();

        return Json(new { success = true, isActive = banner.IsActive });
    }

    /// <summary>
    /// Xóa Banner quảng cáo
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var banner = await _context.Banners.FindAsync(id);
        if (banner != null)
        {
            _context.Banners.Remove(banner);
            await _context.SaveChangesAsync();
            _cacheInvalidation.InvalidateHomePageCache();
            TempData["SuccessMessage"] = "Đã xóa Banner thành công!";
        }

        return RedirectToAction(nameof(Index));
    }
}

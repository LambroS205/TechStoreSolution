using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechStore.Core.Common;
using TechStore.Core.Entities;
using TechStore.Core.Interfaces;
using TechStore.Infrastructure.Data;
using TechStore.Web.Security;

namespace TechStore.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
[HasPermission("Brands.Manage")]
public class BrandController : Controller
{
    private readonly TechStoreDbContext _context;
    private readonly IFileStorageService _fileStorageService;
    private readonly IAuditLogService _auditLogService;

    public BrandController(
        TechStoreDbContext context,
        IFileStorageService fileStorageService,
        IAuditLogService auditLogService)
    {
        _context = context;
        _fileStorageService = fileStorageService;
        _auditLogService = auditLogService;
    }

    /// <summary>
    /// Hiển thị danh sách thương hiệu và số lượng sản phẩm liên quan
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Index(string? search)
    {
        var query = _context.Brands
            .AsNoTracking()
            .Include(b => b.Products)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(b => b.Name.ToLower().Contains(s) || b.Slug.ToLower().Contains(s));
        }

        var brands = await query.OrderBy(b => b.Name).ToListAsync();
        ViewBag.Search = search;

        return View(brands);
    }

    /// <summary>
    /// Thêm mới hoặc cập nhật thương hiệu
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(Brand brand, IFormFile? logoFile)
    {
        if (string.IsNullOrWhiteSpace(brand.Name))
        {
            TempData["ErrorMessage"] = "Tên thương hiệu không được để trống!";
            return RedirectToAction(nameof(Index));
        }

        var slug = string.IsNullOrWhiteSpace(brand.Slug)
            ? SlugHelper.GenerateSlug(brand.Name)
            : SlugHelper.GenerateSlug(brand.Slug);

        // Kiểm tra trùng Slug ngoại trừ chính nó
        bool isDuplicateSlug = await _context.Brands
            .AnyAsync(b => b.Slug == slug && b.BrandId != brand.BrandId);

        if (isDuplicateSlug)
        {
            slug = $"{slug}-{DateTime.UtcNow.Ticks % 10000}";
        }

        string? logoUrl = brand.LogoUrl;
        if (logoFile != null && logoFile.Length > 0)
        {
            using var stream = logoFile.OpenReadStream();
            logoUrl = await _fileStorageService.SaveFileAsync(stream, logoFile.FileName, "brands");
        }

        if (brand.BrandId == 0)
        {
            brand.Slug = slug;
            brand.LogoUrl = logoUrl;

            await _context.Brands.AddAsync(brand);
            await _context.SaveChangesAsync();

            await _auditLogService.LogAsync(
                action: "CreateBrand",
                module: "Brands",
                recordId: brand.BrandId.ToString(),
                oldValues: null,
                newValues: new { brand.Name, brand.Slug, brand.LogoUrl }
            );

            TempData["SuccessMessage"] = $"Đã thêm mới thương hiệu '{brand.Name}' thành công!";
        }
        else
        {
            var existing = await _context.Brands.FindAsync(brand.BrandId);
            if (existing == null) return NotFound();

            var oldValues = new { existing.Name, existing.Slug, existing.LogoUrl, existing.IsActive };

            existing.Name = brand.Name.Trim();
            existing.Slug = slug;
            existing.Description = brand.Description?.Trim();
            existing.IsActive = brand.IsActive;
            if (!string.IsNullOrWhiteSpace(logoUrl))
            {
                existing.LogoUrl = logoUrl;
            }

            await _context.SaveChangesAsync();

            await _auditLogService.LogAsync(
                action: "UpdateBrand",
                module: "Brands",
                recordId: existing.BrandId.ToString(),
                oldValues: oldValues,
                newValues: new { existing.Name, existing.Slug, existing.LogoUrl, existing.IsActive }
            );

            TempData["SuccessMessage"] = $"Đã cập nhật thương hiệu '{existing.Name}' thành công!";
        }

        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Bật/Tắt trạng thái hoạt động qua AJAX
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        var brand = await _context.Brands.FindAsync(id);
        if (brand == null) return Json(new { success = false, message = "Không tìm thấy thương hiệu." });

        brand.IsActive = !brand.IsActive;
        await _context.SaveChangesAsync();

        return Json(new { success = true, isActive = brand.IsActive });
    }

    /// <summary>
    /// Xóa thương hiệu
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var brand = await _context.Brands
            .Include(b => b.Products)
            .FirstOrDefaultAsync(b => b.BrandId == id);

        if (brand == null)
        {
            TempData["ErrorMessage"] = "Thương hiệu không tồn tại.";
            return RedirectToAction(nameof(Index));
        }

        if (brand.Products.Any())
        {
            TempData["ErrorMessage"] = $"Thương hiệu '{brand.Name}' đang có {brand.Products.Count} sản phẩm, không thể xóa trực tiếp!";
            return RedirectToAction(nameof(Index));
        }

        _context.Brands.Remove(brand);
        await _context.SaveChangesAsync();

        await _auditLogService.LogAsync(
            action: "DeleteBrand",
            module: "Brands",
            recordId: id.ToString(),
            oldValues: new { brand.Name, brand.Slug },
            newValues: null
        );

        TempData["SuccessMessage"] = $"Đã xóa thương hiệu '{brand.Name}' thành công!";
        return RedirectToAction(nameof(Index));
    }
}

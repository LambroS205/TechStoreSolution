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
[HasPermission("Categories.Manage")]
public class CategoryController : Controller
{
    private readonly TechStoreDbContext _context;
    private readonly IFileStorageService _fileStorageService;
    private readonly IAuditLogService _auditLogService;

    public CategoryController(
        TechStoreDbContext context,
        IFileStorageService fileStorageService,
        IAuditLogService auditLogService)
    {
        _context = context;
        _fileStorageService = fileStorageService;
        _auditLogService = auditLogService;
    }

    /// <summary>
    /// Hiển thị danh sách danh mục phân cấp và thống kê sản phẩm
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var categories = await _context.Categories
            .AsNoTracking()
            .Include(c => c.ParentCategory)
            .Include(c => c.Products)
            .OrderBy(c => c.ParentId ?? 0)
            .ThenBy(c => c.DisplayOrder)
            .ThenBy(c => c.Name)
            .ToListAsync();

        ViewBag.ParentCategories = categories.Where(c => c.ParentId == null).ToList();

        return View(categories);
    }

    /// <summary>
    /// Thêm mới hoặc Cập nhật danh mục
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(Category category, IFormFile? imageFile)
    {
        if (string.IsNullOrWhiteSpace(category.Name))
        {
            TempData["ErrorMessage"] = "Tên danh mục không được để trống!";
            return RedirectToAction(nameof(Index));
        }

        var slug = string.IsNullOrWhiteSpace(category.Slug)
            ? SlugHelper.GenerateSlug(category.Name)
            : SlugHelper.GenerateSlug(category.Slug);

        // Kiểm tra trùng Slug ngoại trừ chính nó
        bool isDuplicateSlug = await _context.Categories
            .AnyAsync(c => c.Slug == slug && c.CategoryId != category.CategoryId);

        if (isDuplicateSlug)
        {
            slug = $"{slug}-{DateTime.UtcNow.Ticks % 10000}";
        }

        string? imageUrl = category.ImageUrl;
        if (imageFile != null && imageFile.Length > 0)
        {
            using var stream = imageFile.OpenReadStream();
            imageUrl = await _fileStorageService.SaveFileAsync(stream, imageFile.FileName, "categories");
        }

        if (category.CategoryId == 0)
        {
            category.Slug = slug;
            category.ImageUrl = imageUrl;
            if (category.ParentId <= 0) category.ParentId = null;

            await _context.Categories.AddAsync(category);
            await _context.SaveChangesAsync();

            await _auditLogService.LogAsync(
                action: "CreateCategory",
                module: "Categories",
                recordId: category.CategoryId.ToString(),
                oldValues: null,
                newValues: new { category.Name, category.Slug, category.ParentId }
            );

            TempData["SuccessMessage"] = $"Đã thêm mới danh mục '{category.Name}' thành công!";
        }
        else
        {
            // Tránh trường hợp chọn chính mình làm danh mục cha
            if (category.ParentId == category.CategoryId)
            {
                TempData["ErrorMessage"] = "Danh mục không thể tự làm danh mục cha của chính nó!";
                return RedirectToAction(nameof(Index));
            }

            var existing = await _context.Categories.FindAsync(category.CategoryId);
            if (existing == null) return NotFound();

            var oldValues = new { existing.Name, existing.Slug, existing.ParentId, existing.DisplayOrder, existing.IsActive };

            existing.Name = category.Name.Trim();
            existing.Slug = slug;
            existing.ParentId = category.ParentId > 0 ? category.ParentId : null;
            existing.DisplayOrder = category.DisplayOrder;
            existing.Icon = category.Icon?.Trim();
            existing.IsActive = category.IsActive;
            if (!string.IsNullOrWhiteSpace(imageUrl))
            {
                existing.ImageUrl = imageUrl;
            }

            await _context.SaveChangesAsync();

            await _auditLogService.LogAsync(
                action: "UpdateCategory",
                module: "Categories",
                recordId: existing.CategoryId.ToString(),
                oldValues: oldValues,
                newValues: new { existing.Name, existing.Slug, existing.ParentId, existing.DisplayOrder, existing.IsActive }
            );

            TempData["SuccessMessage"] = $"Đã cập nhật danh mục '{existing.Name}' thành công!";
        }

        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Bật/Tắt trạng thái hoạt động qua AJAX
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        var category = await _context.Categories.FindAsync(id);
        if (category == null) return Json(new { success = false, message = "Không tìm thấy danh mục." });

        category.IsActive = !category.IsActive;
        await _context.SaveChangesAsync();

        return Json(new { success = true, isActive = category.IsActive });
    }

    /// <summary>
    /// Xóa danh mục
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var category = await _context.Categories
            .Include(c => c.SubCategories)
            .Include(c => c.Products)
            .FirstOrDefaultAsync(c => c.CategoryId == id);

        if (category == null)
        {
            TempData["ErrorMessage"] = "Danh mục không tồn tại.";
            return RedirectToAction(nameof(Index));
        }

        if (category.SubCategories.Any())
        {
            TempData["ErrorMessage"] = $"Danh mục '{category.Name}' đang chứa các danh mục con. Vui lòng di chuyển hoặc xóa các danh mục con trước!";
            return RedirectToAction(nameof(Index));
        }

        if (category.Products.Any())
        {
            TempData["ErrorMessage"] = $"Danh mục '{category.Name}' đang chứa {category.Products.Count} sản phẩm. Không thể xóa!";
            return RedirectToAction(nameof(Index));
        }

        _context.Categories.Remove(category);
        await _context.SaveChangesAsync();

        await _auditLogService.LogAsync(
            action: "DeleteCategory",
            module: "Categories",
            recordId: id.ToString(),
            oldValues: new { category.Name, category.Slug },
            newValues: null
        );

        TempData["SuccessMessage"] = $"Đã xóa danh mục '{category.Name}' thành công!";
        return RedirectToAction(nameof(Index));
    }
}

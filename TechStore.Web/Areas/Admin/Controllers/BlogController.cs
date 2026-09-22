using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TechStore.Core.Entities;
using TechStore.Core.Interfaces;
using TechStore.Infrastructure.Data;
using TechStore.Web.Security;

namespace TechStore.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
[HasPermission("Blogs.Manage")]
public class BlogController : Controller
{
    private readonly TechStoreDbContext _context;
    private readonly IFileStorageService _fileStorageService;
    private readonly IAuditLogService _auditLogService;

    public BlogController(
        TechStoreDbContext context,
        IFileStorageService fileStorageService,
        IAuditLogService auditLogService)
    {
        _context = context;
        _fileStorageService = fileStorageService;
        _auditLogService = auditLogService;
    }

    /// <summary>
    /// Quản lý danh sách bài viết blog và tin tức công nghệ
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Index(string? q, int? categoryId, int page = 1)
    {
        int pageSize = 12;
        var query = _context.BlogPosts
            .Include(p => p.Category)
            .Include(p => p.Author)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var search = q.Trim().ToLower();
            query = query.Where(p => p.Title.ToLower().Contains(search) || p.Summary.ToLower().Contains(search));
        }

        if (categoryId.HasValue && categoryId.Value > 0)
        {
            query = query.Where(p => p.BlogCategoryId == categoryId.Value);
        }

        int totalItems = await query.CountAsync();
        var posts = await query
            .OrderByDescending(p => p.PublishedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.Search = q;
        ViewBag.CategoryId = categoryId;
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
        ViewBag.TotalItems = totalItems;
        ViewBag.Categories = await _context.BlogCategories.AsNoTracking().OrderBy(c => c.Name).ToListAsync();

        return View(posts);
    }

    /// <summary>
    /// Giao diện tạo mới bài viết
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await LoadCategoriesAsync();
        var model = new BlogPost
        {
            PublishedAt = DateTime.UtcNow,
            IsPublished = true
        };
        return View(model);
    }

    /// <summary>
    /// Xử lý tạo bài viết và tải ảnh bìa
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BlogPost model, IFormFile? thumbnail)
    {
        if (string.IsNullOrWhiteSpace(model.Title))
        {
            ModelState.AddModelError("Title", "Vui lòng nhập tiêu đề bài viết!");
        }

        if (string.IsNullOrWhiteSpace(model.Content))
        {
            ModelState.AddModelError("Content", "Nội dung bài viết không được để trống!");
        }

        if (model.BlogCategoryId <= 0)
        {
            ModelState.AddModelError("BlogCategoryId", "Vui lòng chọn danh mục bài viết!");
        }

        if (!ModelState.IsValid)
        {
            await LoadCategoriesAsync();
            return View(model);
        }

        // Tự động sinh slug nếu chưa có
        if (string.IsNullOrWhiteSpace(model.Slug))
        {
            model.Slug = GenerateSlug(model.Title);
        }
        else
        {
            model.Slug = GenerateSlug(model.Slug);
        }

        // Đảm bảo Slug duy nhất
        string baseSlug = model.Slug;
        int count = 1;
        while (await _context.BlogPosts.AnyAsync(p => p.Slug == model.Slug))
        {
            model.Slug = $"{baseSlug}-{count++}";
        }

        // Tải ảnh đại diện nếu có
        if (thumbnail != null && thumbnail.Length > 0)
        {
            using var stream = thumbnail.OpenReadStream();
            model.ThumbnailUrl = await _fileStorageService.SaveFileAsync(stream, thumbnail.FileName, "blogs");
        }
        else if (string.IsNullOrWhiteSpace(model.ThumbnailUrl))
        {
            model.ThumbnailUrl = "/assets/images/placeholder.png";
        }

        // Lấy AuthorId từ User đăng nhập
        int authorId = 1;
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int uid))
        {
            authorId = uid;
        }
        model.AuthorId = authorId;

        if (model.PublishedAt == default)
        {
            model.PublishedAt = DateTime.UtcNow;
        }

        await _context.BlogPosts.AddAsync(model);
        await _context.SaveChangesAsync();

        await _auditLogService.LogAsync(
            action: "CREATE_BLOG_POST",
            module: "Blogs",
            recordId: model.PostId.ToString(),
            oldValues: null,
            newValues: System.Text.Json.JsonSerializer.Serialize(new { model.PostId, model.Title, model.Slug }));

        TempData["SuccessMessage"] = $"Đã xuất bản bài viết \"{model.Title}\" thành công!";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Giao diện chỉnh sửa bài viết
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var post = await _context.BlogPosts.FindAsync(id);
        if (post == null)
        {
            return NotFound();
        }

        await LoadCategoriesAsync();
        return View(post);
    }

    /// <summary>
    /// Xử lý cập nhật bài viết
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, BlogPost model, IFormFile? thumbnail)
    {
        if (id != model.PostId)
        {
            return BadRequest();
        }

        var post = await _context.BlogPosts.FindAsync(id);
        if (post == null)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(model.Title))
        {
            ModelState.AddModelError("Title", "Vui lòng nhập tiêu đề bài viết!");
        }

        if (string.IsNullOrWhiteSpace(model.Content))
        {
            ModelState.AddModelError("Content", "Nội dung bài viết không được để trống!");
        }

        if (!ModelState.IsValid)
        {
            await LoadCategoriesAsync();
            return View(model);
        }

        string oldValues = System.Text.Json.JsonSerializer.Serialize(new { post.Title, post.Slug, post.IsPublished, post.BlogCategoryId });

        post.Title = model.Title.Trim();
        post.Summary = model.Summary?.Trim() ?? string.Empty;
        post.Content = model.Content;
        post.BlogCategoryId = model.BlogCategoryId;
        post.IsPublished = model.IsPublished;

        if (!string.IsNullOrWhiteSpace(model.Slug))
        {
            string newSlug = GenerateSlug(model.Slug);
            if (newSlug != post.Slug && !await _context.BlogPosts.AnyAsync(p => p.Slug == newSlug && p.PostId != id))
            {
                post.Slug = newSlug;
            }
        }

        // Cập nhật ảnh nếu có file mới
        if (thumbnail != null && thumbnail.Length > 0)
        {
            using var stream = thumbnail.OpenReadStream();
            post.ThumbnailUrl = await _fileStorageService.SaveFileAsync(stream, thumbnail.FileName, "blogs");
        }
        else if (!string.IsNullOrWhiteSpace(model.ThumbnailUrl))
        {
            post.ThumbnailUrl = model.ThumbnailUrl;
        }

        await _context.SaveChangesAsync();

        await _auditLogService.LogAsync(
            action: "UPDATE_BLOG_POST",
            module: "Blogs",
            recordId: post.PostId.ToString(),
            oldValues: oldValues,
            newValues: System.Text.Json.JsonSerializer.Serialize(new { post.Title, post.Slug, post.IsPublished, post.BlogCategoryId }));

        TempData["SuccessMessage"] = $"Đã cập nhật bài viết \"{post.Title}\"!";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Xóa bài viết
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var post = await _context.BlogPosts.FindAsync(id);
        if (post != null)
        {
            string title = post.Title;
            _context.BlogPosts.Remove(post);
            await _context.SaveChangesAsync();

            await _auditLogService.LogAsync(
                action: "DELETE_BLOG_POST",
                module: "Blogs",
                recordId: id.ToString(),
                oldValues: System.Text.Json.JsonSerializer.Serialize(new { post.PostId, post.Title }),
                newValues: null);

            TempData["SuccessMessage"] = $"Đã xóa bài viết \"{title}\"!";
        }
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Bật / Tắt trạng thái xuất bản
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TogglePublish(int id)
    {
        var post = await _context.BlogPosts.FindAsync(id);
        if (post != null)
        {
            post.IsPublished = !post.IsPublished;
            await _context.SaveChangesAsync();

            await _auditLogService.LogAsync(
                action: "TOGGLE_BLOG_PUBLISH",
                module: "Blogs",
                recordId: id.ToString(),
                oldValues: null,
                newValues: System.Text.Json.JsonSerializer.Serialize(new { post.PostId, post.IsPublished }));

            TempData["SuccessMessage"] = $"Đã thay đổi trạng thái bài viết \"{post.Title}\" thành {(post.IsPublished ? "Xuất bản" : "Ẩn")}!";
        }
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Danh sách danh mục bài viết
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Categories()
    {
        var categories = await _context.BlogCategories
            .Include(c => c.Posts)
            .OrderBy(c => c.Name)
            .ToListAsync();
        return View(categories);
    }

    /// <summary>
    /// Thêm hoặc sửa danh mục bài viết
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveCategory(BlogCategory model)
    {
        if (string.IsNullOrWhiteSpace(model.Name))
        {
            TempData["ErrorMessage"] = "Tên danh mục không được để trống!";
            return RedirectToAction(nameof(Categories));
        }

        string slug = string.IsNullOrWhiteSpace(model.Slug) ? GenerateSlug(model.Name) : GenerateSlug(model.Slug);

        if (model.BlogCategoryId == 0)
        {
            model.Slug = slug;
            await _context.BlogCategories.AddAsync(model);
            TempData["SuccessMessage"] = $"Đã tạo chuyên mục \"{model.Name}\"!";
        }
        else
        {
            var cat = await _context.BlogCategories.FindAsync(model.BlogCategoryId);
            if (cat != null)
            {
                cat.Name = model.Name.Trim();
                cat.Slug = slug;
                TempData["SuccessMessage"] = $"Đã cập nhật chuyên mục \"{cat.Name}\"!";
            }
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Categories));
    }

    /// <summary>
    /// Xóa danh mục bài viết
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        var cat = await _context.BlogCategories.Include(c => c.Posts).FirstOrDefaultAsync(c => c.BlogCategoryId == id);
        if (cat != null)
        {
            if (cat.Posts.Any())
            {
                TempData["ErrorMessage"] = "Không thể xóa chuyên mục đang có bài viết!";
            }
            else
            {
                _context.BlogCategories.Remove(cat);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Đã xóa chuyên mục \"{cat.Name}\"!";
            }
        }
        return RedirectToAction(nameof(Categories));
    }

    private async Task LoadCategoriesAsync()
    {
        var categories = await _context.BlogCategories
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .ToListAsync();
        ViewBag.Categories = new SelectList(categories, "BlogCategoryId", "Name");
    }

    private static string GenerateSlug(string phrase)
    {
        string str = phrase.ToLowerInvariant();
        // Thay thế ký tự tiếng Việt có dấu
        string[] vietnameseSigns = new string[]
        {
            "aAeEoOuUiIdDyY",
            "áàạảãâấầậẩẫăắằặẳẵ",
            "ÁÀẠẢÃÂẤẦẬẨẪĂẮẰẶẲẴ",
            "éèẹẻẽêếềệểễ",
            "ÉÈẸẺẼÊẾỀỆỂỄ",
            "óòọỏõôốồộổỗơớờợởỡ",
            "ÓÒỌỎÕÔỐỒỘỔỖƠỚỜỢỞỠ",
            "úùụủũưứừựửữ",
            "ÚÙỤỦŨƯỨỪỰỬỮ",
            "íìịỉĩ",
            "ÍÌỊỈĨ",
            "đ",
            "Đ",
            "ýỳỵỷỹ",
            "ÝỲỴỶỸ"
        };
        for (int i = 1; i < vietnameseSigns.Length; i++)
        {
            for (int j = 0; j < vietnameseSigns[i].Length; j++)
                str = str.Replace(vietnameseSigns[i][j], vietnameseSigns[0][i - 1]);
        }
        // Xóa ký tự đặc biệt
        str = Regex.Replace(str, @"[^a-z0-9\s-]", "");
        // Chuyển nhiều dấu cách thành 1 gạch ngang
        str = Regex.Replace(str, @"\s+", " ").Trim();
        str = Regex.Replace(str, @"\s", "-");
        return str;
    }
}

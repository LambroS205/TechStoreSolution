using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using TechStore.Infrastructure.Data;

namespace TechStore.Web.Controllers;

public class BlogController : Controller
{
    private readonly TechStoreDbContext _context;

    public BlogController(TechStoreDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Trang danh sách bài viết tin tức công nghệ, đánh giá và mẹo vặt
    /// </summary>
    [HttpGet]
    [Route("blogs")]
    [Route("blogs/category/{categorySlug}")]
    public async Task<IActionResult> Index(string? categorySlug = null, int page = 1)
    {
        int pageSize = 9;
        var query = _context.BlogPosts
            .Include(p => p.Category)
            .Include(p => p.Author)
            .AsNoTracking()
            .Where(p => p.IsPublished)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(categorySlug))
        {
            query = query.Where(p => p.Category.Slug == categorySlug);
            var currentCat = await _context.BlogCategories.FirstOrDefaultAsync(c => c.Slug == categorySlug);
            ViewBag.CurrentCategory = currentCat;
        }

        int totalItems = await query.CountAsync();
        var posts = await query
            .OrderByDescending(p => p.PublishedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.Categories = await _context.BlogCategories.AsNoTracking().ToListAsync();
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
        ViewBag.TotalItems = totalItems;
        ViewBag.CategorySlug = categorySlug;

        return View(posts);
    }

    /// <summary>
    /// Trang chi tiết bài viết công nghệ
    /// </summary>
    [HttpGet]
    [Route("blog/{slug}")]
    public async Task<IActionResult> Detail(string slug)
    {
        var post = await _context.BlogPosts
            .Include(p => p.Category)
            .Include(p => p.Author)
            .FirstOrDefaultAsync(p => p.Slug == slug && p.IsPublished);

        if (post == null)
        {
            return NotFound();
        }

        // Tăng lượt xem
        post.ViewCount++;
        await _context.SaveChangesAsync();

        // Lấy 3 bài viết liên quan
        var relatedPosts = await _context.BlogPosts
            .Include(p => p.Category)
            .AsNoTracking()
            .Where(p => p.BlogCategoryId == post.BlogCategoryId && p.PostId != post.PostId && p.IsPublished)
            .OrderByDescending(p => p.PublishedAt)
            .Take(3)
            .ToListAsync();

        ViewBag.RelatedPosts = relatedPosts;
        return View(post);
    }
}

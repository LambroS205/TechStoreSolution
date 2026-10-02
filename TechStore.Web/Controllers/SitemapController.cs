using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechStore.Infrastructure.Data;

namespace TechStore.Web.Controllers;

public class SitemapController : Controller
{
    private readonly TechStoreDbContext _context;

    public SitemapController(TechStoreDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Tự động sinh XML Sitemap chuẩn sitemaps.org cho công cụ tìm kiếm (Google, Bing...)
    /// </summary>
    [HttpGet]
    [Route("sitemap.xml")]
    [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any)] // Cache 1 giờ
    public async Task<IActionResult> Sitemap()
    {
        string baseUrl = $"{Request.Scheme}://{Request.Host}";

        XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
        var root = new XElement(ns + "urlset");

        // 1. Các trang tĩnh cố định
        var staticPages = new (string Path, string ChangeFreq, string Priority)[]
        {
            ("/", "daily", "1.0"),
            ("/products", "daily", "0.9"),
            ("/about", "monthly", "0.5"),
            ("/contact", "monthly", "0.5"),
            ("/blogs", "weekly", "0.7")
        };

        foreach (var page in staticPages)
        {
            root.Add(new XElement(ns + "url",
                new XElement(ns + "loc", $"{baseUrl}{page.Path}"),
                new XElement(ns + "changefreq", page.ChangeFreq),
                new XElement(ns + "priority", page.Priority)
            ));
        }

        // 2. Danh mục sản phẩm (Categories)
        var categories = await _context.Categories
            .AsNoTracking()
            .Where(c => c.IsActive && !string.IsNullOrEmpty(c.Slug))
            .Select(c => new { c.Slug })
            .ToListAsync();

        foreach (var cat in categories)
        {
            root.Add(new XElement(ns + "url",
                new XElement(ns + "loc", $"{baseUrl}/category/{cat.Slug}"),
                new XElement(ns + "changefreq", "daily"),
                new XElement(ns + "priority", "0.8")
            ));
        }

        // 3. Sản phẩm chi tiết (Products)
        var products = await _context.Products
            .AsNoTracking()
            .Where(p => p.IsActive && !string.IsNullOrEmpty(p.Slug))
            .Select(p => new { p.Slug, LastMod = p.UpdatedAt ?? p.CreatedAt })
            .ToListAsync();

        foreach (var prod in products)
        {
            root.Add(new XElement(ns + "url",
                new XElement(ns + "loc", $"{baseUrl}/product/{prod.Slug}"),
                new XElement(ns + "lastmod", prod.LastMod.ToString("yyyy-MM-dd")),
                new XElement(ns + "changefreq", "weekly"),
                new XElement(ns + "priority", "0.9")
            ));
        }

        // 4. Bài viết Blog
        var blogPosts = await _context.BlogPosts
            .AsNoTracking()
            .Where(b => b.IsPublished && !string.IsNullOrEmpty(b.Slug))
            .Select(b => new { b.Slug, LastMod = b.PublishedAt })
            .ToListAsync();

        foreach (var blog in blogPosts)
        {
            root.Add(new XElement(ns + "url",
                new XElement(ns + "loc", $"{baseUrl}/blog/{blog.Slug}"),
                new XElement(ns + "lastmod", blog.LastMod.ToString("yyyy-MM-dd")),
                new XElement(ns + "changefreq", "monthly"),
                new XElement(ns + "priority", "0.6")
            ));
        }

        var doc = new XDocument(new XDeclaration("1.0", "utf-8", "yes"), root);
        return Content(doc.ToString(), "application/xml; charset=utf-8", Encoding.UTF8);
    }
}

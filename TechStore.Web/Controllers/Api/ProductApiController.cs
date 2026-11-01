using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using TechStore.Infrastructure.Data;

namespace TechStore.Web.Controllers.Api;

[ApiController]
[Route("api/product")]
[EnableRateLimiting("ApiPolicy")]
public class ProductApiController : ControllerBase
{
    private readonly TechStoreDbContext _context;

    public ProductApiController(TechStoreDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// API gợi ý sản phẩm tìm kiếm thời gian thực (Autocomplete)
    /// </summary>
    [HttpGet("suggest")]
    public async Task<IActionResult> Suggest([FromQuery] string? q)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Trim().Length < 2)
        {
            return Ok(Array.Empty<object>());
        }

        var keyword = q.Trim().ToLower();

        var products = await _context.Products
            .AsNoTracking()
            .Include(p => p.Brand)
            .Include(p => p.Category)
            .Include(p => p.Variants.Where(v => v.IsActive))
            .Where(p => p.IsActive && (
                p.Name.ToLower().Contains(keyword) ||
                p.Slug.ToLower().Contains(keyword) ||
                p.Brand.Name.ToLower().Contains(keyword) ||
                p.Category.Name.ToLower().Contains(keyword) ||
                p.Variants.Any(v => v.SKU.ToLower().Contains(keyword))
            ))
            .Take(6)
            .Select(p => new
            {
                id = p.ProductId,
                name = p.Name,
                slug = p.Slug,
                image = p.FeaturedImage,
                brand = p.Brand.Name,
                category = p.Category.Name,
                minPrice = p.Variants.Any() ? p.Variants.Min(v => v.SalePrice) : 0,
                formattedPrice = p.Variants.Any() ? p.Variants.Min(v => v.SalePrice).ToString("N0") + " đ" : ""
            })
            .ToListAsync();

        return Ok(products);
    }

    /// <summary>
    /// API lấy thông tin nhanh để hiển thị Modal Quick View (Xem nhanh sản phẩm)
    /// </summary>
    [HttpGet("quick-view/{id:int}")]
    public async Task<IActionResult> QuickView(int id)
    {
        var product = await _context.Products
            .Include(p => p.Brand)
            .Include(p => p.Category)
            .Include(p => p.Variants.Where(v => v.IsActive))
            .Include(p => p.Reviews.Where(r => r.IsApproved))
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.ProductId == id && p.IsActive);

        if (product == null)
        {
            return NotFound(new { success = false, message = "Không tìm thấy sản phẩm" });
        }

        Dictionary<string, string> specs = new();
        if (!string.IsNullOrWhiteSpace(product.SpecificationsJson))
        {
            try
            {
                specs = JsonSerializer.Deserialize<Dictionary<string, string>>(product.SpecificationsJson) ?? new();
            }
            catch { }
        }

        var variants = product.Variants.OrderBy(v => v.SalePrice).Select(v => new
        {
            variantId = v.VariantId,
            sku = v.SKU,
            variantName = v.VariantName,
            originalPrice = v.OriginalPrice,
            salePrice = v.SalePrice,
            stockQuantity = v.StockQuantity,
            thumbnailImage = string.IsNullOrEmpty(v.ThumbnailImage) ? product.FeaturedImage : v.ThumbnailImage
        }).ToList();

        double avgRating = product.Reviews.Any() ? Math.Round(product.Reviews.Average(r => (double)r.Rating), 1) : 5.0;

        return Ok(new
        {
            success = true,
            productId = product.ProductId,
            name = product.Name,
            slug = product.Slug,
            brandName = product.Brand.Name,
            categoryName = product.Category.Name,
            featuredImage = product.FeaturedImage,
            shortDescription = product.ShortDescription,
            warrantyMonths = product.WarrantyMonths,
            averageRating = avgRating,
            totalReviews = product.Reviews.Count,
            specifications = specs.Take(5).ToDictionary(kv => kv.Key, kv => kv.Value),
            variants
        });
    }
}

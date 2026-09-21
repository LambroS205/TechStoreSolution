using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using TechStore.Infrastructure.Data;

namespace TechStore.Web.Controllers.Api;

[ApiController]
[Route("api/product")]
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
}

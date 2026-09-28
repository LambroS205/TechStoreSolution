using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechStore.Core.Entities;
using TechStore.Infrastructure.Data;
using TechStore.Web.Models;

namespace TechStore.Web.Controllers;

/// <summary>
/// Controller xử lý tính năng So Sánh Sản Phẩm chuyên sâu (Product Comparison)
/// </summary>
public class CompareController : Controller
{
    private readonly TechStoreDbContext _context;

    public CompareController(TechStoreDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Hiển thị bảng so sánh thông số chi tiết giữa 2-4 sản phẩm
    /// </summary>
    [HttpGet]
    [Route("compare")]
    public async Task<IActionResult> Index(string? ids)
    {
        var model = new CompareViewModel();

        if (!string.IsNullOrWhiteSpace(ids))
        {
            var idList = ids.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => int.TryParse(s.Trim(), out int val) ? val : 0)
                .Where(id => id > 0)
                .Distinct()
                .Take(4)
                .ToList();

            if (idList.Any())
            {
                var products = await _context.Products
                    .Where(p => idList.Contains(p.ProductId) && p.IsActive)
                    .Include(p => p.Brand)
                    .Include(p => p.Category)
                    .Include(p => p.Variants.Where(v => v.IsActive))
                    .Include(p => p.Reviews)
                    .AsNoTracking()
                    .ToListAsync();

                // Sắp xếp đúng theo thứ tự ID được truyền vào URL
                model.Products = idList
                    .Select(id => products.FirstOrDefault(p => p.ProductId == id))
                    .Where(p => p != null)
                    .Select(p => p!)
                    .ToList();

                // Lấy sản phẩm gợi ý cùng danh mục để bổ sung vào slot trống
                if (model.Products.Any())
                {
                    var catId = model.Products.First().CategoryId;
                    var comparedIds = model.Products.Select(p => p.ProductId).ToList();

                    model.SuggestedProducts = await _context.Products
                        .Where(p => p.CategoryId == catId && !comparedIds.Contains(p.ProductId) && p.IsActive)
                        .Include(p => p.Brand)
                        .Include(p => p.Variants.Where(v => v.IsActive))
                        .Take(8)
                        .AsNoTracking()
                        .ToListAsync();
                }
            }
        }

        return View(model);
    }

    /// <summary>
    /// API tìm kiếm sản phẩm theo từ khóa để thêm trực tiếp vào bảng so sánh
    /// </summary>
    [HttpGet]
    [Route("api/compare/search")]
    public async Task<IActionResult> Search(string? q, int? categoryId)
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            return Json(new List<object>());
        }

        var query = _context.Products
            .Where(p => p.IsActive && p.Name.Contains(q))
            .Include(p => p.Brand)
            .Include(p => p.Variants.Where(v => v.IsActive))
            .AsNoTracking()
            .AsQueryable();

        if (categoryId.HasValue && categoryId.Value > 0)
        {
            query = query.Where(p => p.CategoryId == categoryId.Value);
        }

        var results = await query
            .Take(6)
            .Select(p => new
            {
                p.ProductId,
                p.Name,
                p.Slug,
                BrandName = p.Brand.Name,
                p.FeaturedImage,
                Price = p.Variants.OrderBy(v => v.SalePrice).Select(v => v.SalePrice).FirstOrDefault(),
                OriginalPrice = p.Variants.OrderBy(v => v.SalePrice).Select(v => v.OriginalPrice).FirstOrDefault()
            })
            .ToListAsync();

        return Json(results);
    }
}

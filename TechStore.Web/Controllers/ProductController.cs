using Microsoft.Extensions.Caching.Memory;
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

public class ProductController : Controller
{
    private readonly TechStoreDbContext _context;
    private readonly IMemoryCache _cache;

    public ProductController(TechStoreDbContext context, IMemoryCache cache)
    {
        _context = context;
        _cache = cache;
    }

    /// <summary>
    /// Trang danh sách sản phẩm hỗ trợ bộ lọc: Danh mục, Hãng, RAM, Bộ nhớ lưu trữ, Khoảng giá, Sắp xếp
    /// </summary>
    [HttpGet]
    [Route("products")]
    [Route("category/{categorySlug}")]
    public async Task<IActionResult> Index(
        string? categorySlug,
        string? brand,
        string? storage,
        string? ram,
        decimal? minPrice,
        decimal? maxPrice,
        string? sort,
        string? q,
        int page = 1)
    {
        int pageSize = 12;
        var query = _context.Products
            .Include(p => p.Brand)
            .Include(p => p.Category)
            .Include(p => p.Variants.Where(v => v.IsActive))
            .AsNoTracking()
            .Where(p => p.IsActive)
            .AsQueryable();

        // 1. Lọc theo Danh mục (Category)
        Category? currentCategory = null;
        if (!string.IsNullOrWhiteSpace(categorySlug))
        {
            currentCategory = await _context.Categories.FirstOrDefaultAsync(c => c.Slug == categorySlug);
            if (currentCategory != null)
            {
                query = query.Where(p => p.CategoryId == currentCategory.CategoryId);
            }
        }

        // 2. Lọc theo Từ khóa tìm kiếm (Search Query)
        if (!string.IsNullOrWhiteSpace(q))
        {
            query = query.Where(p => p.Name.Contains(q) || p.ShortDescription!.Contains(q));
            ViewBag.SearchQuery = q;
        }

        // 3. Lọc theo Thương hiệu (Brand)
        if (!string.IsNullOrWhiteSpace(brand))
        {
            query = query.Where(p => p.Brand.Slug == brand);
        }

        // 4. Lọc theo biến thể bộ nhớ (Storage: 256GB, 512GB, 1TB)
        if (!string.IsNullOrWhiteSpace(storage))
        {
            query = query.Where(p => p.Variants.Any(v => v.VariantName.Contains(storage)));
        }

        // 5. Lọc theo biến thể RAM (16GB, 24GB, 32GB)
        if (!string.IsNullOrWhiteSpace(ram))
        {
            query = query.Where(p => p.Variants.Any(v => v.VariantName.Contains(ram)));
        }

        // 6. Lọc theo Khoảng giá bán (Price Range)
        if (minPrice.HasValue)
        {
            query = query.Where(p => p.Variants.Any(v => v.SalePrice >= minPrice.Value));
        }
        if (maxPrice.HasValue)
        {
            query = query.Where(p => p.Variants.Any(v => v.SalePrice <= maxPrice.Value));
        }

        // 7. Sắp xếp kết quả (Sort)
        query = sort switch
        {
            "price_asc" => query.OrderBy(p => p.Variants.Min(v => v.SalePrice)),
            "price_desc" => query.OrderByDescending(p => p.Variants.Max(v => v.SalePrice)),
            "newest" => query.OrderByDescending(p => p.CreatedAt),
            _ => query.OrderByDescending(p => p.IsFeatured).ThenByDescending(p => p.CreatedAt)
        };

        // Phân trang
        int totalItems = await query.CountAsync();
        var products = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // Chuẩn bị dữ liệu cho Sidebar bộ lọc với IMemoryCache (30 phút)
        var allBrands = await _cache.GetOrCreateAsync("all_active_brands", entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30);
            return _context.Brands.AsNoTracking().Where(b => b.IsActive).ToListAsync();
        }) ?? new List<Brand>();

        var allCategories = await _cache.GetOrCreateAsync("all_active_categories", entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30);
            return _context.Categories.AsNoTracking().Where(c => c.IsActive).ToListAsync();
        }) ?? new List<Category>();

        var viewModel = new ProductListViewModel
        {
            Products = products,
            CurrentCategory = currentCategory,
            Brands = allBrands,
            Categories = allCategories,
            CurrentPage = page,
            TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize),
            TotalItems = totalItems,
            SelectedBrand = brand,
            SelectedStorage = storage,
            SelectedRam = ram,
            SelectedMinPrice = minPrice,
            SelectedMaxPrice = maxPrice,
            SelectedSort = sort ?? "default"
        };

        return View(viewModel);
    }

    /// <summary>
    /// Trang chi tiết sản phẩm và các biến thể cấu hình
    /// </summary>
    [HttpGet]
    [Route("product/{slug}")]
    public async Task<IActionResult> Detail(string slug)
    {
        var product = await _context.Products
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Include(p => p.Variants.Where(v => v.IsActive))
            .Include(p => p.Images)
            .AsSplitQuery() // Tối ưu tránh Cartesian Product warning (EF Core 20504)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Slug == slug && p.IsActive);

        if (product == null)
        {
            return NotFound();
        }

        // Tăng lượt xem sản phẩm bằng Atomic ExecuteUpdateAsync không gây lock bảng
        await _context.Products
            .Where(p => p.ProductId == product.ProductId)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.ViewCount, p => p.ViewCount + 1));

        // Lấy sản phẩm tương tự cùng danh mục (sắp xếp rõ ràng trước khi Take để loại bỏ EF Core 10102)
        var relatedProducts = await _context.Products
            .Include(p => p.Variants.Where(v => v.IsActive))
            .AsNoTracking()
            .Where(p => p.CategoryId == product.CategoryId && p.ProductId != product.ProductId && p.IsActive)
            .OrderByDescending(p => p.CreatedAt)
            .Take(4)
            .ToListAsync();

        // Tính toán thống kê đánh giá tối ưu bằng SQL aggregation trực tiếp
        var stats = await _context.ProductReviews
            .Where(r => r.ProductId == product.ProductId && r.IsApproved)
            .GroupBy(r => 1)
            .Select(g => new
            {
                Total = g.Count(),
                Avg = g.Average(r => (double)r.Rating),
                C5 = g.Count(r => r.Rating == 5),
                C4 = g.Count(r => r.Rating == 4),
                C3 = g.Count(r => r.Rating == 3),
                C2 = g.Count(r => r.Rating == 2),
                C1 = g.Count(r => r.Rating == 1)
            })
            .FirstOrDefaultAsync();

        int totalReviews = stats?.Total ?? 0;
        double averageRating = stats != null && stats.Total > 0 ? Math.Round(stats.Avg, 1) : 5.0;
        int count5 = stats?.C5 ?? 0;
        int count4 = stats?.C4 ?? 0;
        int count3 = stats?.C3 ?? 0;
        int count2 = stats?.C2 ?? 0;
        int count1 = stats?.C1 ?? 0;

        // Lấy danh sách đánh giá mới nhất (giới hạn tối đa 20 đánh giá tránh tràn RAM)
        var reviews = await _context.ProductReviews
            .Include(r => r.User)
            .Where(r => r.ProductId == product.ProductId && r.IsApproved)
            .OrderByDescending(r => r.CreatedAt)
            .Take(20)
            .ToListAsync();

        bool hasPurchased = false;
        bool hasReviewed = false;
        if (User.Identity?.IsAuthenticated == true)
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int uid))
            {
                hasPurchased = await _context.Orders
                    .Where(o => o.UserId == uid)
                    .AnyAsync(o => o.OrderDetails.Any(od => od.Variant.ProductId == product.ProductId));
                hasReviewed = await _context.ProductReviews
                    .AnyAsync(r => r.ProductId == product.ProductId && r.UserId == uid);
            }
        }

        var viewModel = new ProductDetailViewModel
        {
            Product = product,
            RelatedProducts = relatedProducts,
            Reviews = reviews,
            TotalReviews = totalReviews,
            AverageRating = averageRating,
            Count5 = count5,
            Count4 = count4,
            Count3 = count3,
            Count2 = count2,
            Count1 = count1,
            HasPurchased = hasPurchased,
            HasReviewed = hasReviewed
        };

        return View(viewModel);
    }

    /// <summary>
    /// Tiếp nhận đánh giá sao và nhận xét của khách hàng cho sản phẩm
    /// </summary>
    [HttpPost]
    [Route("product/{slug}/review")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitReview(string slug, [FromForm] int rating, [FromForm] string? comment)
    {
        var product = await _context.Products.FirstOrDefaultAsync(p => p.Slug == slug && p.IsActive);
        if (product == null) return NotFound();

        if (rating < 1 || rating > 5)
        {
            TempData["ErrorMessage"] = "Vui lòng chọn số sao đánh giá từ 1 đến 5 sao!";
            return RedirectToAction(nameof(Detail), new { slug });
        }

        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
        {
            return RedirectToAction("Login", "Auth", new { returnUrl = $"/product/{slug}" });
        }

        string? cleanComment = comment?.Trim();
        if (cleanComment?.Length > 1000)
        {
            cleanComment = cleanComment[..1000];
        }

        // Kiểm tra xem người dùng đã từng đánh giá sản phẩm này chưa
        var existingReview = await _context.ProductReviews
            .FirstOrDefaultAsync(r => r.ProductId == product.ProductId && r.UserId == userId);

        if (existingReview != null)
        {
            existingReview.Rating = rating;
            existingReview.Comment = cleanComment;
            existingReview.CreatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Bạn đã cập nhật đánh giá cho sản phẩm thành công!";
        }
        else
        {
            var review = new ProductReview
            {
                ProductId = product.ProductId,
                UserId = userId,
                Rating = rating,
                Comment = cleanComment,
                IsApproved = true,
                CreatedAt = DateTime.UtcNow
            };

            await _context.ProductReviews.AddAsync(review);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Cảm ơn bạn đã gửi đánh giá cho sản phẩm!";
        }

        return RedirectToAction(nameof(Detail), new { slug });
    }
}
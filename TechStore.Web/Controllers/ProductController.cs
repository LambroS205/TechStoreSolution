using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechStore.Core.Entities;
using TechStore.Infrastructure.Data;

namespace TechStore.Web.Controllers;

public class ProductController : Controller
{
    private readonly TechStoreDbContext _context;

    public ProductController(TechStoreDbContext context)
    {
        _context = context;
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

        // Chuẩn bị dữ liệu cho Sidebar bộ lọc
        var allBrands = await _context.Brands.Where(b => b.IsActive).ToListAsync();
        var allCategories = await _context.Categories.Where(c => c.IsActive).ToListAsync();

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
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Slug == slug && p.IsActive);

        if (product == null)
        {
            return NotFound();
        }

        // Tăng lượt xem sản phẩm
        var dbProduct = await _context.Products.FindAsync(product.ProductId);
        if (dbProduct != null)
        {
            dbProduct.ViewCount++;
            await _context.SaveChangesAsync();
        }

        // Lấy sản phẩm tương tự cùng danh mục
        var relatedProducts = await _context.Products
            .Include(p => p.Variants.Where(v => v.IsActive))
            .AsNoTracking()
            .Where(p => p.CategoryId == product.CategoryId && p.ProductId != product.ProductId && p.IsActive)
            .Take(4)
            .ToListAsync();

        ViewBag.RelatedProducts = relatedProducts;

        // Lấy danh sách đánh giá sản phẩm & tính toán thống kê
        var reviews = await _context.ProductReviews
            .Include(r => r.User)
            .Where(r => r.ProductId == product.ProductId && r.IsApproved)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        int totalReviews = reviews.Count;
        double averageRating = reviews.Any() ? Math.Round(reviews.Average(r => r.Rating), 1) : 5.0;
        int count5 = reviews.Count(r => r.Rating == 5);
        int count4 = reviews.Count(r => r.Rating == 4);
        int count3 = reviews.Count(r => r.Rating == 3);
        int count2 = reviews.Count(r => r.Rating == 2);
        int count1 = reviews.Count(r => r.Rating == 1);

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
                hasReviewed = reviews.Any(r => r.UserId == uid);
            }
        }

        ViewBag.Reviews = reviews;
        ViewBag.TotalReviews = totalReviews;
        ViewBag.AverageRating = averageRating;
        ViewBag.Count5 = count5;
        ViewBag.Count4 = count4;
        ViewBag.Count3 = count3;
        ViewBag.Count2 = count2;
        ViewBag.Count1 = count1;
        ViewBag.HasPurchased = hasPurchased;
        ViewBag.HasReviewed = hasReviewed;

        return View(product);
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

        var review = new ProductReview
        {
            ProductId = product.ProductId,
            UserId = userId,
            Rating = rating,
            Comment = comment?.Trim(),
            IsApproved = true,
            CreatedAt = DateTime.UtcNow
        };

        await _context.ProductReviews.AddAsync(review);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Cảm ơn bạn đã gửi đánh giá cho sản phẩm!";
        return RedirectToAction(nameof(Detail), new { slug });
    }
}

public class ProductListViewModel
{
    public List<Product> Products { get; set; } = new();
    public Category? CurrentCategory { get; set; }
    public List<Brand> Brands { get; set; } = new();
    public List<Category> Categories { get; set; } = new();
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public int TotalItems { get; set; }

    public string? SelectedBrand { get; set; }
    public string? SelectedStorage { get; set; }
    public string? SelectedRam { get; set; }
    public decimal? SelectedMinPrice { get; set; }
    public decimal? SelectedMaxPrice { get; set; }
    public string SelectedSort { get; set; } = "default";
}
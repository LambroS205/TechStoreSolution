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

public class HomeController : Controller
{
    private readonly TechStoreDbContext _context;
    private readonly IMemoryCache _cache;

    public HomeController(TechStoreDbContext context, IMemoryCache cache)
    {
        _context = context;
        _cache = cache;
    }

    /// <summary>
    /// Trang chủ hiển thị Hero Banner động, Danh mục sản phẩm, Flash Sale, Voucher & Thiết bị công nghệ nổi bật
    /// </summary>
    public async Task<IActionResult> Index()
    {
        var viewModel = await _cache.GetOrCreateAsync("home_page_viewmodel", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);

            // 1. Lấy danh sách Slider Hero chính đang hoạt động
            var heroSliders = await _context.Banners
                .AsNoTracking()
                .Where(b => b.Position == "HomeHeroSlider" && b.IsActive)
                .OrderBy(b => b.DisplayOrder)
                .ToListAsync();

            // 2. Lấy Banner phụ cạnh slider
            var subBanners = await _context.Banners
                .AsNoTracking()
                .Where(b => b.Position == "HomeSubBanner" && b.IsActive)
                .OrderBy(b => b.DisplayOrder)
                .Take(2)
                .ToListAsync();

            // 3. Danh mục sản phẩm công nghệ nổi bật
            var categories = await _context.Categories
                .AsNoTracking()
                .Where(c => c.IsActive)
                .OrderBy(c => c.DisplayOrder)
                .ToListAsync();

            // 4. Danh sách thiết bị công nghệ Flagship nổi bật kèm các biến thể giá
            var featuredProducts = await _context.Products
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .Include(p => p.Variants.Where(v => v.IsActive))
                .Include(p => p.Reviews.Where(r => r.IsApproved))
                .AsNoTracking()
                .Where(p => p.IsActive && p.IsFeatured)
                .OrderByDescending(p => p.CreatedAt)
                .Take(8)
                .ToListAsync();

            // 5. Flash Sale: Top 4 sản phẩm có mức giảm giá tốt nhất
            var allSaleCandidates = await _context.Products
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .Include(p => p.Variants.Where(v => v.IsActive))
                .Include(p => p.Reviews.Where(r => r.IsApproved))
                .AsNoTracking()
                .Where(p => p.IsActive && p.Variants.Any(v => v.IsActive && v.OriginalPrice > v.SalePrice))
                .ToListAsync();

            var flashSaleProducts = allSaleCandidates
                .OrderByDescending(p =>
                {
                    var minV = p.Variants.OrderBy(v => v.SalePrice).FirstOrDefault();
                    if (minV == null || minV.OriginalPrice <= minV.SalePrice) return 0;
                    return (minV.OriginalPrice - minV.SalePrice) / minV.OriginalPrice;
                })
                .Take(4)
                .ToList();

            // 6. Danh sách thương hiệu đối tác
            var brands = await _context.Brands
                .AsNoTracking()
                .Where(b => b.IsActive)
                .OrderBy(b => b.BrandId)
                .ToListAsync();

            // 7. Mã giảm giá đang hoạt động công khai
            var activeCoupons = await _context.Coupons
                .AsNoTracking()
                .Where(c => c.IsActive && c.EndDate > DateTime.UtcNow && c.UsageCount < c.UsageLimit)
                .OrderByDescending(c => c.DiscountValue)
                .Take(4)
                .ToListAsync();

            // 8. Tin tức & bài viết đánh giá công nghệ mới nhất
            var latestBlogs = await _context.BlogPosts
                .Include(b => b.Category)
                .AsNoTracking()
                .Where(b => b.IsPublished)
                .OrderByDescending(b => b.PublishedAt)
                .Take(3)
                .ToListAsync();

            return new HomeViewModel
            {
                HeroSliders = heroSliders,
                SubBanners = subBanners,
                Categories = categories,
                FeaturedProducts = featuredProducts,
                FlashSaleProducts = flashSaleProducts,
                Brands = brands,
                ActiveCoupons = activeCoupons,
                LatestBlogPosts = latestBlogs
            };
        });

        return View(viewModel);
    }

    [Route("/about")]
    public IActionResult About() => View();

    [HttpGet]
    [Route("/contact")]
    public IActionResult Contact() => View(new ContactFormViewModel());

    [HttpPost]
    [Route("/contact")]
    [ValidateAntiForgeryToken]
    public IActionResult Contact(ContactFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (string.IsNullOrWhiteSpace(model.FullName) || string.IsNullOrWhiteSpace(model.Email) || string.IsNullOrWhiteSpace(model.Message))
        {
            ModelState.AddModelError(string.Empty, "Vui lòng điền đầy đủ các thông tin bắt buộc!");
            return View(model);
        }

        TempData["SuccessMessage"] = "Cảm ơn bạn đã gửi liên hệ! Đội ngũ Chăm sóc khách hàng TechStore sẽ phản hồi bạn trong vòng 24 giờ qua Email hoặc Số điện thoại.";
        return RedirectToAction(nameof(Contact));
    }

    [HttpGet]
    [HttpPost]
    [Route("/error/{code:int}")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult StatusCodeHandler(int code)
    {
        Response.StatusCode = code;
        if (code == 404)
        {
            return View("NotFound");
        }

        return View("Error", new TechStore.Web.Models.ErrorViewModel
        {
            StatusCode = code,
            RequestId = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier,
            Message = code switch
            {
                401 => "Bạn cần đăng nhập để truy cập trang này.",
                403 => "Bạn không có quyền truy cập vào nội dung này.",
                405 => "Phương thức HTTP không được hỗ trợ cho tài nguyên này (Method Not Allowed).",
                500 => "Máy chủ gặp sự cố ngoài ý muốn. Vui lòng thử lại sau.",
                _ => "Đã có lỗi xảy ra trong quá trình xử lý yêu cầu."
            }
        });
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new TechStore.Web.Models.ErrorViewModel
        {
            StatusCode = 500,
            RequestId = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier,
            Message = "Đã xảy ra lỗi ngoài ý muốn. Đội ngũ kỹ thuật đang kiểm tra xử lý."
        });
    }
}
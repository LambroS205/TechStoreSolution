using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechStore.Core.Entities;
using TechStore.Infrastructure.Data;

namespace TechStore.Web.Controllers;

public class HomeController : Controller
{
    private readonly TechStoreDbContext _context;

    public HomeController(TechStoreDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Trang chủ hiển thị Hero Banner động, Danh mục sản phẩm & Thiết bị công nghệ nổi bật
    /// </summary>
    public async Task<IActionResult> Index()
    {
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
            .AsNoTracking()
            .Where(p => p.IsActive && p.IsFeatured)
            .OrderByDescending(p => p.CreatedAt)
            .Take(8)
            .ToListAsync();

        // 5. Danh sách thương hiệu đối tác
        var brands = await _context.Brands
            .AsNoTracking()
            .Where(b => b.IsActive)
            .ToListAsync();

        var viewModel = new HomeViewModel
        {
            HeroSliders = heroSliders,
            SubBanners = subBanners,
            Categories = categories,
            FeaturedProducts = featuredProducts,
            Brands = brands
        };

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

public class ContactFormViewModel
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public class HomeViewModel
{
    public List<Banner> HeroSliders { get; set; } = new();
    public List<Banner> SubBanners { get; set; } = new();
    public List<Category> Categories { get; set; } = new();
    public List<Product> FeaturedProducts { get; set; } = new();
    public List<Brand> Brands { get; set; } = new();
}
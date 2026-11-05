using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;

namespace TechStore.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
public class HomeController : Controller
{
    private readonly IAuthorizationService _authorizationService;

    public HomeController(IAuthorizationService authorizationService)
    {
        _authorizationService = authorizationService;
    }

    /// <summary>
    /// Smart Role Router: Tự động phát hiện và điều hướng người dùng tới phân hệ họ được cấp quyền
    /// </summary>
    [HttpGet]
    [HttpPost]
    public async Task<IActionResult> Index()
    {
        // 1. SuperAdmin luôn vào Dashboard
        if (User.IsInRole("SuperAdmin"))
        {
            return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
        }

        // 2. Chuyên trách Marketing -> Luôn vào phân hệ CMS Banner & Chiến dịch
        if (User.IsInRole("StaffMarketing") || (await _authorizationService.AuthorizeAsync(User, "Permission:Banners.Manage")).Succeeded)
        {
            return RedirectToAction("Index", "Banner", new { area = "Admin" });
        }

        // 3. Chuyên trách Kho hàng
        if (User.IsInRole("StaffInventory") || (await _authorizationService.AuthorizeAsync(User, "Permission:Inventory.View")).Succeeded)
        {
            return RedirectToAction("Index", "Inventory", new { area = "Admin" });
        }

        // 4. Chuyên trách Bán hàng & Đơn hàng
        if (User.IsInRole("StaffOrder") || (await _authorizationService.AuthorizeAsync(User, "Permission:Orders.View")).Succeeded)
        {
            return RedirectToAction("Index", "Order", new { area = "Admin" });
        }

        // 5. Kiểm tra theo thứ tự ưu tiên các phân hệ khác
        if ((await _authorizationService.AuthorizeAsync(User, "Permission:Dashboard.View")).Succeeded)
        {
            return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
        }

        if ((await _authorizationService.AuthorizeAsync(User, "Permission:Products.View")).Succeeded)
        {
            return RedirectToAction("Index", "Product", new { area = "Admin" });
        }

        if ((await _authorizationService.AuthorizeAsync(User, "Permission:Coupons.View")).Succeeded || (await _authorizationService.AuthorizeAsync(User, "Permission:Coupons.Manage")).Succeeded)
        {
            return RedirectToAction("Index", "Coupon", new { area = "Admin" });
        }

        if ((await _authorizationService.AuthorizeAsync(User, "Permission:Blogs.View")).Succeeded || (await _authorizationService.AuthorizeAsync(User, "Permission:Blogs.Manage")).Succeeded)
        {
            return RedirectToAction("Index", "Blog", new { area = "Admin" });
        }

        if ((await _authorizationService.AuthorizeAsync(User, "Permission:Reviews.View")).Succeeded || (await _authorizationService.AuthorizeAsync(User, "Permission:Reviews.Manage")).Succeeded)
        {
            return RedirectToAction("Index", "Review", new { area = "Admin" });
        }

        if ((await _authorizationService.AuthorizeAsync(User, "Permission:Users.View")).Succeeded)
        {
            return RedirectToAction("Index", "User", new { area = "Admin" });
        }

        if ((await _authorizationService.AuthorizeAsync(User, "Permission:AuditLog.View")).Succeeded || (await _authorizationService.AuthorizeAsync(User, "Permission:AuditLogs.View")).Succeeded)
        {
            return RedirectToAction("Index", "AuditLog", new { area = "Admin" });
        }

        if ((await _authorizationService.AuthorizeAsync(User, "Permission:Settings.Manage")).Succeeded)
        {
            return RedirectToAction("Index", "SiteSettings", new { area = "Admin" });
        }

        return RedirectToAction(nameof(AccessDenied));
    }

    /// <summary>
    /// Trang thông báo 403 Access Denied thân thiện cho phân hệ Quản trị
    /// </summary>
    [HttpGet]
    [HttpPost]
    [AllowAnonymous]
    [Route("Admin/AccessDenied")]
    public IActionResult AccessDenied()
    {
        Response.StatusCode = 403;
        return View();
    }
}


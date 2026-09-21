using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechStore.Core.Entities;
using TechStore.Core.Interfaces;
using TechStore.Infrastructure.Data;

namespace TechStore.Web.Controllers;

[Authorize]
public class AccountController : Controller
{
    private readonly TechStoreDbContext _context;
    private readonly IFileStorageService _fileStorageService;
    private readonly IAuditLogService _auditLogService;
    private readonly PasswordHasher<User> _passwordHasher;

    public AccountController(
        TechStoreDbContext context,
        IFileStorageService fileStorageService,
        IAuditLogService auditLogService)
    {
        _context = context;
        _fileStorageService = fileStorageService;
        _auditLogService = auditLogService;
        _passwordHasher = new PasswordHasher<User>();
    }

    /// <summary>
    /// Trang hồ sơ cá nhân và quản lý tài khoản khách hàng
    /// </summary>
    [HttpGet]
    [Route("account/profile")]
    public async Task<IActionResult> Profile()
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return RedirectToAction("Login", "Auth");

        var totalOrders = await _context.Orders
            .AsNoTracking()
            .CountAsync(o => o.UserId == user.UserId);

        var totalSpent = await _context.Orders
            .AsNoTracking()
            .Where(o => o.UserId == user.UserId && (o.PaymentStatus == "Paid" || o.OrderStatus == "Delivered"))
            .SumAsync(o => (decimal?)o.TotalAmount) ?? 0m;

        var activeOrders = await _context.Orders
            .AsNoTracking()
            .CountAsync(o => o.UserId == user.UserId && (o.OrderStatus == "Pending" || o.OrderStatus == "Processing" || o.OrderStatus == "Shipping"));

        var viewModel = new ProfileViewModel
        {
            UserId = user.UserId,
            Username = user.Username,
            FullName = user.FullName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            AvatarUrl = user.AvatarUrl,
            CreatedAt = user.CreatedAt,
            TotalOrders = totalOrders,
            TotalSpent = totalSpent,
            ActiveOrdersCount = activeOrders,
            RoleName = user.UserRoles.FirstOrDefault()?.Role.RoleName ?? "Thành viên"
        };

        return View(viewModel);
    }

    /// <summary>
    /// Cập nhật thông tin cá nhân (Họ tên, SĐT, Avatar)
    /// </summary>
    [HttpPost]
    [Route("account/update-profile")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProfile(ProfileUpdateDto dto, IFormFile? avatarFile)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return RedirectToAction("Login", "Auth");

        if (string.IsNullOrWhiteSpace(dto.FullName))
        {
            TempData["ErrorMessage"] = "Họ và tên không được để trống!";
            return RedirectToAction(nameof(Profile));
        }

        user.FullName = dto.FullName.Trim();
        user.PhoneNumber = dto.PhoneNumber?.Trim();

        if (avatarFile != null && avatarFile.Length > 0)
        {
            using var stream = avatarFile.OpenReadStream();
            user.AvatarUrl = await _fileStorageService.SaveFileAsync(stream, avatarFile.FileName, "avatars");
        }

        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        // Làm mới Cookie Authentication để hiển thị FullName mới trên Header
        await RefreshSignInAsync(user);

        TempData["SuccessMessage"] = "Đã cập nhật thông tin cá nhân thành công!";
        return RedirectToAction(nameof(Profile));
    }

    /// <summary>
    /// Đổi mật khẩu tài khoản
    /// </summary>
    [HttpPost]
    [Route("account/change-password")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordDto dto)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return RedirectToAction("Login", "Auth");

        if (string.IsNullOrWhiteSpace(dto.CurrentPassword) || string.IsNullOrWhiteSpace(dto.NewPassword))
        {
            TempData["ErrorMessage"] = "Vui lòng nhập đầy đủ thông tin mật khẩu!";
            return RedirectToAction(nameof(Profile));
        }

        if (dto.NewPassword.Length < 6)
        {
            TempData["ErrorMessage"] = "Mật khẩu mới phải có ít nhất 6 ký tự!";
            return RedirectToAction(nameof(Profile));
        }

        if (dto.NewPassword != dto.ConfirmPassword)
        {
            TempData["ErrorMessage"] = "Mật khẩu xác nhận không khớp!";
            return RedirectToAction(nameof(Profile));
        }

        // Kiểm tra mật khẩu hiện tại
        bool isPasswordValid = false;
        if (user.PasswordHash == dto.CurrentPassword)
        {
            isPasswordValid = true;
        }
        else
        {
            try
            {
                var verifyResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, dto.CurrentPassword);
                isPasswordValid = verifyResult == PasswordVerificationResult.Success ||
                                  verifyResult == PasswordVerificationResult.SuccessRehashNeeded;
            }
            catch
            {
                isPasswordValid = false;
            }
        }

        if (!isPasswordValid)
        {
            TempData["ErrorMessage"] = "Mật khẩu hiện tại không chính xác!";
            return RedirectToAction(nameof(Profile));
        }

        user.PasswordHash = _passwordHasher.HashPassword(user, dto.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await _auditLogService.LogAsync("ChangePassword", "Security", user.UserId.ToString(), null, new { Username = user.Username });

        TempData["SuccessMessage"] = "Đổi mật khẩu thành công!";
        return RedirectToAction(nameof(Profile));
    }

    /// <summary>
    /// Danh sách đơn hàng đã đặt của khách hàng
    /// </summary>
    [HttpGet]
    [Route("account/orders")]
    public async Task<IActionResult> Orders(string? status)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return RedirectToAction("Login", "Auth");

        var query = _context.Orders
            .AsNoTracking()
            .Include(o => o.OrderDetails)
            .Where(o => o.UserId == user.UserId || (o.CustomerEmail == user.Email && !string.IsNullOrEmpty(user.Email)))
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status) && status != "All")
        {
            query = query.Where(o => o.OrderStatus == status);
        }

        var orders = await query
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        ViewBag.CurrentStatus = status ?? "All";
        return View(orders);
    }

    /// <summary>
    /// Chi tiết đơn hàng của khách hàng
    /// </summary>
    [HttpGet]
    [Route("account/order/{orderCode}")]
    public async Task<IActionResult> OrderDetail(string orderCode)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return RedirectToAction("Login", "Auth");

        var order = await _context.Orders
            .AsNoTracking()
            .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Variant)
                    .ThenInclude(v => v.Product)
            .FirstOrDefaultAsync(o => o.OrderCode == orderCode);

        if (order == null) return NotFound();

        // Kiểm tra quyền sở hữu đơn hàng (Người mua hoặc SuperAdmin)
        bool isOwner = order.UserId == user.UserId || (order.CustomerEmail == user.Email && !string.IsNullOrEmpty(user.Email));
        bool isStaff = User.IsInRole("SuperAdmin") || User.IsInRole("StaffOrder");

        if (!isOwner && !isStaff)
        {
            return Forbid();
        }

        return View(order);
    }

    private async Task<User?> GetCurrentUserAsync()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
        {
            return null;
        }

        return await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.UserId == userId);
    }

    private async Task RefreshSignInAsync(User user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Name, user.Username),
            new("FullName", user.FullName),
            new(ClaimTypes.Email, user.Email)
        };

        foreach (var ur in user.UserRoles)
        {
            claims.Add(new Claim(ClaimTypes.Role, ur.Role.RoleName));
        }

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTime.UtcNow.AddDays(14)
        });
    }
}

public class ProfileViewModel
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? AvatarUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public int TotalOrders { get; set; }
    public decimal TotalSpent { get; set; }
    public int ActiveOrdersCount { get; set; }
    public string RoleName { get; set; } = string.Empty;
}

public class ProfileUpdateDto
{
    public string FullName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
}

public class ChangePasswordDto
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}

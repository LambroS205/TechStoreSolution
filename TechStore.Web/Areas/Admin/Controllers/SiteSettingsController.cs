using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TechStore.Core.Interfaces;
using TechStore.Web.Areas.Admin.Models;
using TechStore.Web.Security;

namespace TechStore.Web.Areas.Admin.Controllers;

/// <summary>
/// Quản trị phân hệ CMS: Cài đặt giao diện động, nhận diện thương hiệu, VietQR và CSS tùy biến
/// </summary>
[Area("Admin")]
[Authorize]
[HasPermission("Settings.Manage")]
public class SiteSettingsController : Controller
{
    private readonly ISiteSettingsService _settingsService;
    private readonly IFileStorageService _fileStorage;
    private readonly IAuditLogService _auditLog;

    public SiteSettingsController(
        ISiteSettingsService settingsService,
        IFileStorageService fileStorage,
        IAuditLogService auditLog)
    {
        _settingsService = settingsService;
        _fileStorage = fileStorage;
        _auditLog = auditLog;
    }

    /// <summary>
    /// Hiển thị giao diện quản trị cấu hình hệ thống (Tabbed CMS UI)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var settings = await _settingsService.GetAllSettingsAsync();

        var model = new SiteSettingsViewModel
        {
            // General
            SiteName = settings.GetValueOrDefault("General.SiteName", "TechStore - Thế Giới Công Nghệ Đỉnh Cao"),
            Hotline = settings.GetValueOrDefault("General.Hotline", "1800 6868"),
            Email = settings.GetValueOrDefault("General.Email", "support@techstore.vn"),
            Address = settings.GetValueOrDefault("General.Address", "123 Đường Công Nghệ, Quận 1, TP. Hồ Chí Minh"),
            WorkingHours = settings.GetValueOrDefault("General.WorkingHours", "8:00 - 21:30 hàng ngày (kể cả CN)"),

            // Branding & Colors
            LogoUrl = settings.GetValueOrDefault("Brand.LogoUrl", ""),
            FaviconUrl = settings.GetValueOrDefault("Brand.FaviconUrl", "/favicon.ico"),
            PrimaryColor = settings.GetValueOrDefault("Theme.PrimaryColor", "#2563eb"),
            SecondaryColor = settings.GetValueOrDefault("Theme.SecondaryColor", "#4f46e5"),
            AccentColor = settings.GetValueOrDefault("Theme.AccentColor", "#f59e0b"),

            // VietQR
            VietQrBankId = settings.GetValueOrDefault("VietQr.BankId", "970422"),
            VietQrBankName = settings.GetValueOrDefault("VietQr.BankName", "MBBank (Ngân hàng Quân Đội)"),
            VietQrAccountNo = settings.GetValueOrDefault("VietQr.AccountNo", "0869162534"),
            VietQrAccountName = settings.GetValueOrDefault("VietQr.AccountName", "CONG TY TNHH TECHSTORE VIET NAM"),
            VietQrTemplate = settings.GetValueOrDefault("VietQr.Template", "compact2"),

            // Footer
            FooterAboutText = settings.GetValueOrDefault("Footer.AboutText", "Hệ thống bán lẻ thiết bị điện tử, laptop, điện thoại, phụ kiện công nghệ chính hãng hàng đầu Việt Nam."),
            FooterCopyright = settings.GetValueOrDefault("Footer.Copyright", "© 2026 TechStore Electronics. Mọi quyền được bảo lưu."),
            FacebookUrl = settings.GetValueOrDefault("Footer.FacebookUrl", "https://facebook.com"),
            YoutubeUrl = settings.GetValueOrDefault("Footer.YoutubeUrl", "https://youtube.com"),
            TiktokUrl = settings.GetValueOrDefault("Footer.TiktokUrl", "https://tiktok.com"),

            // Custom Code
            CustomCss = settings.GetValueOrDefault("CustomCss.Code", ""),
            CustomHeaderHtml = settings.GetValueOrDefault("CustomCss.CustomHeaderHtml", "")
        };

        return View(model);
    }

    /// <summary>
    /// Lưu toàn bộ thay đổi cấu hình từ quản trị viên
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(
        SiteSettingsViewModel model,
        IFormFile? logoFile,
        IFormFile? faviconFile)
    {
        // 1. Xử lý tải ảnh Logo nếu có
        if (logoFile != null && logoFile.Length > 0)
        {
            var allowedExtensions = new[] { ".png", ".jpg", ".jpeg", ".svg", ".webp" };
            var ext = Path.GetExtension(logoFile.FileName).ToLowerInvariant();
            if (Array.IndexOf(allowedExtensions, ext) >= 0)
            {
                using var stream = logoFile.OpenReadStream();
                model.LogoUrl = await _fileStorage.SaveFileAsync(stream, logoFile.FileName, "settings");
            }
        }

        // 2. Xử lý tải ảnh Favicon nếu có
        if (faviconFile != null && faviconFile.Length > 0)
        {
            var allowedExtensions = new[] { ".ico", ".png", ".svg" };
            var ext = Path.GetExtension(faviconFile.FileName).ToLowerInvariant();
            if (Array.IndexOf(allowedExtensions, ext) >= 0)
            {
                using var stream = faviconFile.OpenReadStream();
                model.FaviconUrl = await _fileStorage.SaveFileAsync(stream, faviconFile.FileName, "settings");
            }
        }

        // 3. Chuẩn hóa dữ liệu Dictionary để lưu vào CSDL
        var settingsDict = new Dictionary<string, string?>
        {
            // General
            ["General.SiteName"] = model.SiteName?.Trim(),
            ["General.Hotline"] = model.Hotline?.Trim(),
            ["General.Email"] = model.Email?.Trim(),
            ["General.Address"] = model.Address?.Trim(),
            ["General.WorkingHours"] = model.WorkingHours?.Trim(),

            // Branding
            ["Brand.LogoUrl"] = model.LogoUrl?.Trim(),
            ["Brand.FaviconUrl"] = model.FaviconUrl?.Trim(),
            ["Theme.PrimaryColor"] = model.PrimaryColor?.Trim() ?? "#2563eb",
            ["Theme.SecondaryColor"] = model.SecondaryColor?.Trim() ?? "#4f46e5",
            ["Theme.AccentColor"] = model.AccentColor?.Trim() ?? "#f59e0b",

            // VietQR
            ["VietQr.BankId"] = model.VietQrBankId?.Trim() ?? "970422",
            ["VietQr.BankName"] = model.VietQrBankName?.Trim() ?? "MBBank (Ngân hàng Quân Đội)",
            ["VietQr.AccountNo"] = model.VietQrAccountNo?.Trim() ?? "0869162534",
            ["VietQr.AccountName"] = model.VietQrAccountName?.Trim() ?? "CONG TY TNHH TECHSTORE VIET NAM",
            ["VietQr.Template"] = model.VietQrTemplate?.Trim() ?? "compact2",

            // Footer
            ["Footer.AboutText"] = model.FooterAboutText?.Trim(),
            ["Footer.Copyright"] = model.FooterCopyright?.Trim(),
            ["Footer.FacebookUrl"] = model.FacebookUrl?.Trim(),
            ["Footer.YoutubeUrl"] = model.YoutubeUrl?.Trim(),
            ["Footer.TiktokUrl"] = model.TiktokUrl?.Trim(),

            // Custom Code
            ["CustomCss.Code"] = model.CustomCss,
            ["CustomCss.CustomHeaderHtml"] = model.CustomHeaderHtml
        };

        var username = User.Identity?.Name ?? "Admin";
        var success = await _settingsService.UpdateSettingsBatchAsync(settingsDict, username);

        if (success)
        {
            await _auditLog.LogAsync("Update", "SiteSettings", "All", null, settingsDict);
            TempData["SuccessMessage"] = "Đã lưu và cập nhật toàn bộ cấu hình giao diện & hệ thống thành công!";
        }
        else
        {
            TempData["ErrorMessage"] = "Có lỗi xảy ra khi lưu thiết lập vào cơ sở dữ liệu. Vui lòng thử lại!";
        }

        return RedirectToAction(nameof(Index));
    }
}

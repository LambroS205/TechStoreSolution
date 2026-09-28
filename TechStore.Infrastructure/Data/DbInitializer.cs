using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TechStore.Core.Entities;

namespace TechStore.Infrastructure.Data;

/// <summary>
/// Khởi tạo và sửa chữa dữ liệu chuẩn Unicode UTF-8 tiếng Việt cho hệ thống
/// </summary>
public static class DbInitializer
{
    public static async Task InitializeAndRepairEncodingAsync(TechStoreDbContext context)
    {
        try
        {
            // 1. Danh sách thiết lập chuẩn tiếng Việt không bị lỗi font/mojibake
            var correctSettings = new Dictionary<string, (string Value, string Group, string Description, string ValueType)>
            {
                ["General.SiteName"] = ("TechStore - Thế Giới Công Nghệ Đỉnh Cao", "General", "Tên hiển thị của website", "text"),
                ["General.Hotline"] = ("1800 6868", "General", "Số điện thoại đường dây nóng", "text"),
                ["General.Email"] = ("support@techstore.vn", "General", "Email chăm sóc khách hàng", "text"),
                ["General.Address"] = ("123 Đường Công Nghệ, Quận 1, TP. Hồ Chí Minh", "General", "Địa chỉ trụ sở chính", "text"),
                ["General.WorkingHours"] = ("8:00 - 21:30 hàng ngày (kể cả CN)", "General", "Giờ mở cửa phục vụ", "text"),
                ["Brand.LogoUrl"] = ("", "Branding", "Đường dẫn URL ảnh Logo thương hiệu", "image"),
                ["Brand.FaviconUrl"] = ("/favicon.ico", "Branding", "Biểu tượng Favicon trên tab trình duyệt", "image"),
                ["Theme.PrimaryColor"] = ("#2563eb", "Branding", "Màu sắc chủ đạo website (Primary)", "color"),
                ["Theme.SecondaryColor"] = ("#4f46e5", "Branding", "Màu phụ đạo gradient (Secondary)", "color"),
                ["Theme.AccentColor"] = ("#f59e0b", "Branding", "Màu điểm nhấn (Accent/Badge)", "color"),
                ["VietQr.BankId"] = ("970422", "VietQR", "Mã BIN ngân hàng NAPAS (MBBank: 970422)", "text"),
                ["VietQr.BankName"] = ("MBBank (Ngân hàng Quân Đội)", "VietQR", "Tên ngân hàng thụ hưởng", "text"),
                ["VietQr.AccountNo"] = ("0869162534", "VietQR", "Số tài khoản ngân hàng nhận tiền", "text"),
                ["VietQr.AccountName"] = ("CONG TY TNHH TECHSTORE VIET NAM", "VietQR", "Tên chủ tài khoản in hoa không dấu", "text"),
                ["VietQr.Template"] = ("compact2", "VietQR", "Giao diện mẫu VietQR (compact2, print, qr_only)", "text"),
                ["Footer.AboutText"] = ("Hệ thống bán lẻ thiết bị điện tử, laptop, điện thoại, phụ kiện công nghệ chính hãng hàng đầu Việt Nam.", "Footer", "Mô tả ngắn ở chân trang", "textarea"),
                ["Footer.Copyright"] = ("© 2026 TechStore Electronics. Mọi quyền được bảo lưu.", "Footer", "Dòng chữ bản quyền dưới cùng", "text"),
                ["Footer.FacebookUrl"] = ("https://facebook.com", "Footer", "Liên kết Fanpage Facebook", "text"),
                ["Footer.YoutubeUrl"] = ("https://youtube.com", "Footer", "Liên kết kênh YouTube", "text"),
                ["Footer.TiktokUrl"] = ("https://tiktok.com", "Footer", "Liên kết kênh TikTok", "text"),
                ["CustomCss.Code"] = ("/* Tuỳ biến CSS giao diện tại đây */", "CustomCss", "Mã CSS bổ sung chèn trực tiếp vào Storefront", "code"),
                ["CustomCss.CustomHeaderHtml"] = ("", "CustomCss", "Mã HTML/Script chèn vào thẻ <head>", "code")
            };

            var existingSettings = await context.SiteSettings.ToListAsync();
            var existingDict = existingSettings.ToDictionary(s => s.Key, StringComparer.OrdinalIgnoreCase);

            foreach (var kvp in correctSettings)
            {
                if (existingDict.TryGetValue(kvp.Key, out var existing))
                {
                    // Nếu dữ liệu bị lỗi font (chứa các chuỗi byte lạ như "Tháº¿" hoặc "?") hoặc rỗng với các trường quan trọng
                    if (existing.Value != null && (existing.Value.Contains("áº") || existing.Value.Contains("Ã") || existing.Value.Contains("Ä") || existing.Value.Contains("Â") || existing.Value.Contains("?")))
                    {
                        existing.Value = kvp.Value.Value;
                        existing.Group = kvp.Value.Group;
                        existing.Description = kvp.Value.Description;
                        existing.ValueType = kvp.Value.ValueType;
                        existing.UpdatedAt = DateTime.UtcNow;
                    }
                }
                else
                {
                    context.SiteSettings.Add(new SiteSetting
                    {
                        Key = kvp.Key,
                        Value = kvp.Value.Value,
                        Group = kvp.Value.Group,
                        Description = kvp.Value.Description,
                        ValueType = kvp.Value.ValueType,
                        UpdatedAt = DateTime.UtcNow
                    });
                }
            }

            // 2. Sửa lỗi mã quyền Settings.Manage nếu có
            var perm = await context.Permissions.FirstOrDefaultAsync(p => p.PermissionCode == "Settings.Manage");
            if (perm != null && (perm.Description.Contains("áº") || perm.Description.Contains("Ã") || perm.Description.Contains("Ä")))
            {
                perm.Description = "Quản lý cấu hình giao diện và hệ thống website";
            }

            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DbInitializer Error]: {ex.Message}");
        }
    }
}

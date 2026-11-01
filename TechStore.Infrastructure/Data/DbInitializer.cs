using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TechStore.Core.Entities;

namespace TechStore.Infrastructure.Data;

/// <summary>
/// Khởi tạo và sửa chữa dữ liệu chuẩn Unicode UTF-8, cấu hình, dữ liệu thông số kỹ thuật và tài khoản demo
/// </summary>
public static class DbInitializer
{
    public static async Task InitializeAndRepairEncodingAsync(TechStoreDbContext context)
    {
        try
        {
            // 0. Đảm bảo cột SpecificationsJson tồn tại trong bảng Products (idempotent DDL)
            try
            {
                await context.Database.ExecuteSqlRawAsync(
                    "IF COL_LENGTH('Products', 'SpecificationsJson') IS NULL " +
                    "ALTER TABLE Products ADD SpecificationsJson NVARCHAR(MAX) NULL;");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DbInitializer DDL Note]: {ex.Message}");
            }

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
                ["Theme.PrimaryColor"] = ("#0046be", "Branding", "Màu sắc chủ đạo website (Best Buy Blue)", "color"),
                ["Theme.SecondaryColor"] = ("#002e7a", "Branding", "Màu phụ đạo gradient (Dark Blue)", "color"),
                ["Theme.AccentColor"] = ("#fff200", "Branding", "Màu điểm nhấn (Best Buy Yellow)", "color"),
                ["VietQr.BankId"] = ("970422", "VietQR", "Mã BIN ngân hàng NAPAS (MBBank: 970422)", "text"),
                ["VietQr.BankName"] = ("MBBank (Ngân hàng Quân Đội)", "VietQR", "Tên ngân hàng thụ hưởng", "text"),
                ["VietQr.AccountNo"] = ("0869162534", "VietQR", "Số tài khoản ngân hàng nhận tiền", "text"),
                ["VietQr.AccountName"] = ("CONG TY TNHH TECHSTORE VIET NAM", "VietQR", "Tên chủ tài khoản in hoa không dấu", "text"),
                ["VietQr.Template"] = ("compact2", "VietQR", "Giao diện mẫu VietQR (compact2, print, qr_only)", "text"),
                ["Footer.AboutText"] = ("Hệ thống bán lẻ thiết bị công nghệ chính hãng hàng đầu theo tiêu chuẩn chuỗi cửa hàng Best Buy. Cam kết 100% thiết bị mới, bảo hành chính hãng toàn quốc.", "Footer", "Mô tả ngắn ở chân trang", "textarea"),
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

            // 3. SỬA LỖI URL BANNER: Chuyển toàn bộ "/san-pham/..." sang "/product/..."
            var banners = await context.Banners.ToListAsync();
            foreach (var b in banners)
            {
                if (!string.IsNullOrEmpty(b.TargetUrl) && b.TargetUrl.StartsWith("/san-pham/"))
                {
                    b.TargetUrl = b.TargetUrl.Replace("/san-pham/", "/product/");
                }
            }

            // 4. SỬA LỖI 404 ẢNH TRONG BẢNG ProductImages
            var brokenImages = await context.ProductImages
                .Where(pi => pi.ImageUrl.Contains("cellphones.com.vn"))
                .ToListAsync();

            var replacementImages = new[]
            {
                "https://images.unsplash.com/photo-1695048133142-1a20484d2569?auto=format&fit=crop&w=800&q=80",
                "https://images.unsplash.com/photo-1510557880182-3d4d3cba35a5?auto=format&fit=crop&w=800&q=80",
                "https://images.unsplash.com/photo-1530319067432-f2a729c03db5?auto=format&fit=crop&w=800&q=80"
            };

            for (int i = 0; i < brokenImages.Count; i++)
            {
                brokenImages[i].ImageUrl = replacementImages[i % replacementImages.Length];
            }

            // 5. CẬP NHẬT THÔNG SỐ KỸ THUẬT (SpecificationsJson) CHO CÁC SẢN PHẨM TRONG CSDL
            var products = await context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .ToListAsync();

            foreach (var p in products)
            {
                if (string.IsNullOrWhiteSpace(p.SpecificationsJson))
                {
                    p.SpecificationsJson = GenerateProductSpecifications(p);
                }
            }

            // 6. ĐẢM BẢO TÀI KHOẢN KHÁCH HÀNG VIP DEMO (vipcustomer / Customer@123)
            var vipUser = await context.Users.FirstOrDefaultAsync(u => u.NormalizedUsername == "VIPCUSTOMER");
            var hasher = new PasswordHasher<User>();
            if (vipUser == null)
            {
                vipUser = new User
                {
                    Username = "vipcustomer",
                    NormalizedUsername = "VIPCUSTOMER",
                    FullName = "Nguyễn Văn Demo (VIP)",
                    Email = "vipcustomer@techstore.vn",
                    NormalizedEmail = "VIPCUSTOMER@TECHSTORE.VN",
                    PhoneNumber = "0909123456",
                    EmailConfirmed = true,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                vipUser.PasswordHash = hasher.HashPassword(vipUser, "Customer@123");
                context.Users.Add(vipUser);
                await context.SaveChangesAsync();

                // Gán quyền Customer
                var customerRole = await context.Roles.FirstOrDefaultAsync(r => r.NormalizedName == "CUSTOMER")
                                   ?? await context.Roles.FirstOrDefaultAsync(r => r.RoleId == 5);
                if (customerRole != null)
                {
                    context.UserRoles.Add(new UserRole
                    {
                        UserId = vipUser.UserId,
                        RoleId = customerRole.RoleId
                    });
                }

                // Gán địa chỉ mặc định
                context.CustomerAddresses.Add(new CustomerAddress
                {
                    UserId = vipUser.UserId,
                    RecipientName = "Nguyễn Văn Demo (VIP)",
                    PhoneNumber = "0909123456",
                    Province = "TP. Hồ Chí Minh",
                    District = "Quận 1",
                    Ward = "Phường Bến Nghé",
                    StreetAddress = "123 Đường Lê Lợi",
                    IsDefault = true
                });
            }

            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DbInitializer Error]: {ex.Message}");
        }
    }

    private static string GenerateProductSpecifications(Product p)
    {
        var specs = new Dictionary<string, string>();
        string slug = p.Slug.ToLowerInvariant();
        string catSlug = p.Category?.Slug?.ToLowerInvariant() ?? "";

        if (slug.Contains("iphone-16-pro-max"))
        {
            specs["Màn hình"] = "6.9 inch Super Retina XDR OLED, 120Hz ProMotion, 2000 nits, Dynamic Island";
            specs["Vi xử lý"] = "Apple A18 Pro (3nm thế hệ mới, 6-core GPU)";
            specs["RAM"] = "8GB Unified Memory";
            specs["Bộ nhớ trong"] = "256GB / 512GB / 1TB NVMe tốc độ cao";
            specs["Camera sau"] = "Chính 48MP Fusion + Góc rộng 48MP + Tele 5x 12MP (Camera Control)";
            specs["Camera trước"] = "12MP TrueDepth, quay 4K60 Dolby Vision";
            specs["Pin & Sạc"] = "4.685 mAh, Sạc nhanh 50% trong 30 phút, MagSafe 25W";
            specs["Trọng lượng"] = "227 g (Khung Titan Grade 5 siêu bền nhẹ)";
            specs["Hệ điều hành"] = "iOS 18 (Hỗ trợ Apple Intelligence toàn diện)";
        }
        else if (slug.Contains("macbook-pro-16-m4"))
        {
            specs["Màn hình"] = "16.2 inch Liquid Retina XDR, 1600 nits, ProMotion 120Hz, 3456 x 2234";
            specs["Vi xử lý"] = "Apple M4 Pro (14-core CPU, 20-core GPU, 16-core Neural Engine)";
            specs["RAM"] = "24GB / 48GB Unified Memory (Băng thông 273GB/s)";
            specs["Ổ cứng SSD"] = "512GB / 1TB PCIe 4.0 siêu tốc (đọc 7.4GB/s)";
            specs["Cổng kết nối"] = "3x Thunderbolt 5, HDMI 2.1 8K, SDXC, MagSafe 3, 3.5mm Headphone";
            specs["Pin & Sạc"] = "100Wh, Thời lượng pin kỷ lục lên tới 24 giờ, Sạc nhanh 140W USB-C";
            specs["Trọng lượng"] = "2.14 kg (Vỏ nhôm nguyên khối tái chế 100%)";
            specs["Hệ điều hành"] = "macOS Sequoia";
        }
        else if (slug.Contains("samsung-galaxy-s25-ultra") || slug.Contains("s25-ultra"))
        {
            specs["Màn hình"] = "6.8 inch Dynamic AMOLED 2X, 120Hz, 2600 nits, Kính Gorilla Armor chống chói";
            specs["Vi xử lý"] = "Qualcomm Snapdragon 8 Elite for Galaxy (3nm)";
            specs["RAM"] = "12GB / 16GB LPDDR5X";
            specs["Bộ nhớ trong"] = "256GB / 512GB UFS 4.0";
            specs["Camera sau"] = "Chính 200MP OIS + Tiềm vọng 50MP 5x + Tele 10MP 3x + Góc siêu rộng 50MP";
            specs["Camera trước"] = "12MP Dual Pixel AF";
            specs["Pin & Sạc"] = "5.000 mAh, Sạc nhanh 45W, Sạc không dây 15W, Chia sẻ pin ngược";
            specs["Trọng lượng"] = "219 g (Khung viền Titan cao cấp, tích hợp bút S-Pen quyền năng)";
            specs["Hệ điều hành"] = "One UI 7 (Android 15, Hỗ trợ Galaxy AI 7 năm cập nhật)";
        }
        else if (slug.Contains("sony-wh-1000xm5"))
        {
            specs["Kiểu dáng"] = "Tai nghe chụp tai Over-ear chống ồn chủ động Flagship";
            specs["Màng loa"] = "30mm màng loa sợi carbon tổng hợp siêu nhẹ cứng cáp";
            specs["Công nghệ chống ồn"] = "Bộ đôi chip V1 + QN1 điều khiển hệ thống 8 micro độc quyền";
            specs["Thời lượng pin"] = "30 giờ (bật ANC), 40 giờ (tắt ANC), sạc nhanh 3 phút nghe 3 giờ";
            specs["Kết nối"] = "Bluetooth 5.2, Codec LDAC Hi-Res Wireless, AAC, SBC, dây 3.5mm AUX";
            specs["Tính năng đặc biệt"] = "Nói để trò chuyện (Speak-to-Chat), Âm thanh 360 Reality Audio, Multipoint";
            specs["Trọng lượng"] = "250 g (Đệm da mềm mại giảm áp lực vành tai)";
        }
        else if (catSlug.Contains("dien-thoai") || slug.Contains("phone") || slug.Contains("galaxy") || slug.Contains("xiaomi"))
        {
            specs["Màn hình"] = "6.7 inch OLED / AMOLED 120Hz, Độ sáng cao HDR";
            specs["Vi xử lý"] = "Chip 8 nhân thế hệ mới tiến trình 3nm/4nm tiết kiệm năng lượng";
            specs["RAM"] = "8GB - 12GB LPDDR5X";
            specs["Bộ nhớ trong"] = "128GB / 256GB / 512GB";
            specs["Camera sau"] = "Cụm đa camera 50MP AI chống rung OIS, quay video 4K/8K";
            specs["Camera trước"] = "12MP - 32MP làm đẹp tự nhiên";
            specs["Pin & Sạc"] = "4.500 - 5.000 mAh, Hỗ trợ sạc siêu nhanh";
            specs["Hệ điều hành"] = "Hệ điều hành di động mới nhất 2026";
        }
        else if (catSlug.Contains("laptop") || slug.Contains("macbook") || slug.Contains("laptop") || slug.Contains("asus") || slug.Contains("dell") || slug.Contains("lenovo"))
        {
            specs["Màn hình"] = "14 - 16 inch IPS / OLED 100% sRGB / DCI-P3 sắc nét";
            specs["Vi xử lý"] = "CPU hiệu năng cao thế hệ mới nhất cho đa nhiệm chuyên sâu";
            specs["RAM"] = "16GB - 32GB RAM DDR5";
            specs["Ổ cứng SSD"] = "512GB - 1TB SSD NVMe M.2 tốc độ cao";
            specs["Cổng kết nối"] = "Thunderbolt / USB-C, USB-A, HDMI, Jack 3.5mm";
            specs["Bàn phím & Touchpad"] = "Bàn phím có đèn nền LED, Touchpad phủ kính đa điểm rộng rãi";
            specs["Pin"] = "Thời lượng pin từ 10 - 18 giờ làm việc liên tục";
            specs["Trọng lượng"] = "1.3 - 2.1 kg (Thiết kế mỏng nhẹ sang trọng)";
        }
        else if (catSlug.Contains("tablet") || slug.Contains("ipad") || slug.Contains("tab"))
        {
            specs["Màn hình"] = "11 - 13 inch Liquid Retina / AMOLED 120Hz hỗ trợ bút cảm ứng";
            specs["Vi xử lý"] = "Chip xử lý đồ họa mạnh mẽ tối ưu cho sáng tạo nội dung";
            specs["RAM"] = "8GB - 16GB";
            specs["Bộ nhớ trong"] = "128GB - 512GB";
            specs["Camera"] = "Camera trước Ultra Wide hỗ trợ Center Stage, Camera sau 12MP";
            specs["Pin & Sạc"] = "Dùng liên tục 10 - 14 giờ lướt web và xem video";
            specs["Trọng lượng"] = "450 - 580 g siêu mỏng";
        }
        else if (catSlug.Contains("am-thanh") || slug.Contains("tai-nghe") || slug.Contains("marshall") || slug.Contains("jbl"))
        {
            specs["Kiểu kết nối"] = "Bluetooth 5.3 chuẩn không dây độ trễ thấp & Cổng AUX 3.5mm";
            specs["Chất âm"] = "Âm bass sâu uy lực, âm treble chi tiết, cân bằng dải tần";
            specs["Thời lượng pin"] = "20 - 30 giờ chơi nhạc liên tục";
            specs["Chống nước & Bụi"] = "Chuẩn kháng nước IPX4 - IP67 bền bỉ";
            specs["Tiện ích"] = "Hỗ trợ sạc nhanh qua cổng Type-C, mic đàm thoại khử tiếng ồn";
        }
        else
        {
            specs["Kích thước màn hình"] = "27 - 65 inch độ phân giải 4K Ultra HD / 5K sắc nét";
            specs["Tấm nền"] = "IPS / OLED / QD-Mini LED màu sắc chân thực";
            specs["Tần số quét"] = "120Hz - 240Hz chuyển động mượt mà";
            specs["Cổng kết nối"] = "HDMI 2.1, DisplayPort, Type-C Power Delivery";
            specs["Bảo hành"] = "24 - 36 tháng chính hãng tại nhà";
        }

        return JsonSerializer.Serialize(specs);
    }
}

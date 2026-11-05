using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TechStore.Core.Entities;

namespace TechStore.Infrastructure.Data;

public static partial class DbInitializer
{
    /// <summary>
    /// Bổ sung toàn diện dữ liệu bán lẻ công nghệ phục vụ thuyết trình, demo và chấm điểm đồ án:
    /// 1. Thương hiệu & Danh mục bổ sung (Google, HP, OPPO, Acer, Harman Kardon)
    /// 2. Banners quảng cáo Hero Slider chuyển động tự động
    /// 3. 15 Nhà cung cấp & phân phối chính hãng công nghệ hàng đầu Việt Nam
    /// 4. Tài khoản Nhân viên các bộ phận (Quản lý kho, Điều phối đơn, Marketing & CSKH)
    /// 5. 16 Khách hàng thân thiết với hồ sơ chi tiết và sổ địa chỉ giao hàng khắp các tỉnh thành
    /// 6. 18 Siêu phẩm công nghệ 2026 mới kèm đầy đủ biến thể, màu sắc, hình ảnh & thông số kỹ thuật chuẩn
    /// 7. 25 Đơn hàng thực tế đa trạng thái (Pending, Confirmed, Shipping, Delivered) kèm chi tiết & lịch sử trạng thái
    /// 8. 30+ Đánh giá & nhận xét sản phẩm tích cực
    /// 9. Lịch sử biến động xuất nhập tồn kho
    /// 10. Bộ sưu tập Mã giảm giá (Vouchers/Coupons) khuyến mãi phong phú
    /// 11. Bài viết Blog công nghệ mới nhất
    /// </summary>
    public static async Task SeedComprehensiveRetailDataAsync(TechStoreDbContext context)
    {
        var hasher = new PasswordHasher<User>();

        // 1. Bổ sung Thương hiệu (Brands)
        await SeedBrandsAsync(context);

        // 1.1 Bổ sung Banner Hero Slider
        await SeedBannersAsync(context);

        // 2. Bổ sung Nhà cung cấp (Suppliers) & Cập nhật cho sản phẩm hiện có
        await SeedSuppliersAsync(context);

        // 3. Bổ sung Tài khoản Nhân viên (Staff Users & Roles)
        var staffList = await SeedStaffUsersAsync(context, hasher);

        // 4. Bổ sung Khách hàng & Sổ địa chỉ (Customers & Addresses)
        var customerList = await SeedCustomersAndAddressesAsync(context, hasher);

        // 5. Bổ sung Sản phẩm mới (Products, Variants, Images, Specs)
        await SeedNewProductsAsync(context);

        // 6. Bổ sung Đơn hàng, Chi tiết đơn hàng & Lịch sử trạng thái (Orders)
        await SeedOrdersAsync(context, customerList, staffList);

        // 7. Bổ sung Đánh giá sản phẩm (ProductReviews)
        await SeedProductReviewsAsync(context, customerList);

        // 8. Bổ sung Biến động kho (InventoryTransactions)
        await SeedInventoryTransactionsAsync(context, staffList);

        // 9. Bổ sung Mã giảm giá (Coupons)
        await SeedCouponsAsync(context);

        // 10. Bổ sung Bài viết tin tức công nghệ (BlogPosts)
        await SeedBlogPostsAsync(context, staffList);
    }

    #region 1. Brands
    private static async Task SeedBrandsAsync(TechStoreDbContext context)
    {
        var newBrands = new (string Name, string Slug)[]
        {
            ("Google", "google"),
            ("HP", "hp"),
            ("OPPO", "oppo"),
            ("Acer", "acer"),
            ("Harman Kardon", "harman-kardon")
        };

        foreach (var (name, slug) in newBrands)
        {
            if (!await context.Brands.AnyAsync(b => b.Slug == slug))
            {
                context.Brands.Add(new Brand
                {
                    Name = name,
                    Slug = slug
                });
            }
        }
        await context.SaveChangesAsync();
    }
    #endregion

    #region 1.1 Banners
    private static async Task SeedBannersAsync(TechStoreDbContext context)
    {
        var heroBanners = new (string Title, string Subtitle, string Img, string Target, string Pos, int Order)[]
        {
            ("GALAXY Z FLIP6 & S25 ULTRA", "Kỷ nguyên Galaxy AI đỉnh cao - Trợ giá thu cũ đổi mới tới 5 triệu đồng", "https://images.unsplash.com/photo-1592899677977-9c10ca588bbd?auto=format&fit=crop&w=1600&q=85", "/product/samsung-galaxy-z-flip6", "HomeHeroSlider", 4),
            ("ĐẠI TIỆC ÂM THANH MARSHALL & JBL", "Chất âm đỉnh cao khuấy động mọi bữa tiệc - Giảm trực tiếp 15%", "https://images.unsplash.com/photo-1545454675-3531b543be5d?auto=format&fit=crop&w=1600&q=85", "/product/marshall-emberton-ii", "HomeHeroSlider", 5)
        };

        foreach (var b in heroBanners)
        {
            if (!await context.Banners.AnyAsync(x => x.Title == b.Title))
            {
                context.Banners.Add(new Banner
                {
                    Title = b.Title,
                    Subtitle = b.Subtitle,
                    ImageUrl = b.Img,
                    TargetUrl = b.Target,
                    Position = b.Pos,
                    DisplayOrder = b.Order,
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow
                });
            }
        }
        await context.SaveChangesAsync();
    }
    #endregion

    #region 2. Suppliers
    private static async Task SeedSuppliersAsync(TechStoreDbContext context)
    {
        var suppliersData = new (string Name, string? Contact, string Phone, string? Email, string? Address)[]
        {
            ("Công ty TNHH Apple Việt Nam", "Phạm Minh Tuấn", "18001127", "supply@apple.vn", "Tòa nhà Deutsches Haus, 33 Lê Duẩn, P. Bến Nghé, Quận 1, TP. HCM"),
            ("Samsung Electronics Vietnam (SEV)", "Trần Quốc Hùng", "02839151234", "b2b@samsung.com.vn", "Tòa nhà Bitexco Financial Tower, Bến Nghé, Quận 1, TP. HCM"),
            ("FPT Synnex Distribution (Synnex FPT)", "Lê Hoàng FPT", "02473006666", "contact@synnexfpt.com.vn", "Tòa nhà FPT, Phố Duy Tân, Dịch Vọng Hậu, Cầu Giấy, Hà Nội"),
            ("Công ty TNHH ASUS ROG Vietnam", "Trần Minh Quang", "02838383838", "sales@asus.vn", "Lầu 8, Tòa nhà Viettel Complex, 285 Cách Mạng Tháng 8, Quận 10, TP. HCM"),
            ("Digiworld Corporation (DGW)", "Đoàn Hồng Việt", "02839290059", "info@digiworld.com.vn", "195 Điện Biên Phủ, Phường 15, Bình Thạnh, TP. HCM"),
            ("Petrosetco Distribution (PSD)", "Nguyễn Văn Dầu Khí", "02839118888", "info@psd.com.vn", "Lầu 6, Tòa nhà PetroVietnam, 1-5 Lê Duẩn, Quận 1, TP. HCM"),
            ("Sony Electronics Vietnam Co., Ltd.", "Đặng Quang Nam", "1800588885", "sev.support@sony.com", "Lầu 6, President Place, 93 Nguyễn Du, Quận 1, TP. HCM"),
            ("LG Electronics Vietnam", "Vũ Hoàng Long", "18001503", "b2b.lge@lge.com", "Tòa nhà Keangnam Landmark 72, Mễ Trì, Nam Từ Liêm, Hà Nội"),
            ("Công ty TNHH Quốc tế Viễn Sơn", "Lê Văn Sơn", "02838326085", "info@vienson.com.vn", "162 Bùi Thị Xuân, Phường Phạm Ngũ Lão, Quận 1, TP. HCM"),
            ("Công ty TNHH Tin học Mai Hoàng", "Mai Thế Hoàng", "02436285868", "maihoang@maihoang.com.vn", "241 Phố Vọng, Phường Đồng Tâm, Hai Bà Trưng, Hà Nội"),
            ("Công ty Cổ phần Công nghệ Elite", "Trịnh Quốc Huy", "02835123959", "contact@elite-jsc.com", "289 Điện Biên Phủ, Phường 7, Quận 3, TP. HCM"),
            ("Ash Sound Vietnam (Marshall Official)", "Nguyễn Đức Anh", "02838447799", "support@ash.vn", "38 Trương Quốc Dung, Phường 8, Phú Nhuận, TP. HCM"),
            ("Công ty TNHH Phúc Giang (PGI - JBL & Harman)", "Nguyễn Phúc Giang", "02839299999", "info@pgi.com.vn", "398 Điện Biên Phủ, Phường 11, Quận 10, TP. HCM"),
            ("Xiaomi Communications Vietnam", "Trần Kiến Quốc", "18006888", "service.vn@support.mi.com", "Lầu 18, Tòa nhà Vincom Center Đồng Khởi, Quận 1, TP. HCM"),
            ("Công ty TNHH Thương mại Vĩnh Xuân (SPC Technology)", "Phạm Vĩnh Xuân", "02437280366", "spc@spc.com.vn", "9 ngõ 30 Lý Nam Đế, Cửa Đông, Hoàn Kiếm, Hà Nội")
        };

        foreach (var item in suppliersData)
        {
            var existing = await context.Suppliers.FirstOrDefaultAsync(s => s.Name.Contains(item.Name.Substring(0, 15)) || s.Phone == item.Phone);
            if (existing == null)
            {
                context.Suppliers.Add(new Supplier
                {
                    Name = item.Name,
                    ContactPerson = item.Contact,
                    Phone = item.Phone,
                    Email = item.Email,
                    Address = item.Address,
                    IsActive = true
                });
            }
        }
        await context.SaveChangesAsync();

        // Cập nhật SupplierId cho các sản phẩm chưa có SupplierId
        var defaultApple = await context.Suppliers.FirstOrDefaultAsync(s => s.Name.Contains("Apple"));
        var defaultSamsung = await context.Suppliers.FirstOrDefaultAsync(s => s.Name.Contains("Samsung"));
        var defaultFpt = await context.Suppliers.FirstOrDefaultAsync(s => s.Name.Contains("FPT") || s.Name.Contains("Synnex"));
        var defaultAsus = await context.Suppliers.FirstOrDefaultAsync(s => s.Name.Contains("ASUS"));
        var defaultSony = await context.Suppliers.FirstOrDefaultAsync(s => s.Name.Contains("Sony"));
        var defaultDgw = await context.Suppliers.FirstOrDefaultAsync(s => s.Name.Contains("Digiworld"));
        var defaultLg = await context.Suppliers.FirstOrDefaultAsync(s => s.Name.Contains("LG"));
        var defaultAsh = await context.Suppliers.FirstOrDefaultAsync(s => s.Name.Contains("Ash") || s.Name.Contains("Marshall"));
        var defaultPgi = await context.Suppliers.FirstOrDefaultAsync(s => s.Name.Contains("Phúc Giang") || s.Name.Contains("PGI"));
        var defaultSpc = await context.Suppliers.FirstOrDefaultAsync(s => s.Name.Contains("Vĩnh Xuân") || s.Name.Contains("SPC"));

        var nullSupplierProducts = await context.Products.Include(p => p.Brand).Where(p => p.SupplierId == null).ToListAsync();
        foreach (var p in nullSupplierProducts)
        {
            string bSlug = p.Brand?.Slug?.ToLowerInvariant() ?? "";
            if (bSlug == "apple") p.SupplierId = defaultApple?.SupplierId ?? defaultFpt?.SupplierId;
            else if (bSlug == "samsung") p.SupplierId = defaultSamsung?.SupplierId ?? defaultDgw?.SupplierId;
            else if (bSlug == "asus") p.SupplierId = defaultAsus?.SupplierId;
            else if (bSlug == "sony") p.SupplierId = defaultSony?.SupplierId;
            else if (bSlug == "xiaomi") p.SupplierId = defaultDgw?.SupplierId;
            else if (bSlug == "lg") p.SupplierId = defaultLg?.SupplierId;
            else if (bSlug == "dell" || bSlug == "lenovo") p.SupplierId = defaultSpc?.SupplierId ?? defaultFpt?.SupplierId;
            else if (bSlug == "marshall") p.SupplierId = defaultAsh?.SupplierId;
            else if (bSlug == "jbl") p.SupplierId = defaultPgi?.SupplierId;
            else p.SupplierId = defaultFpt?.SupplierId;
        }
        await context.SaveChangesAsync();
    }
    #endregion

    #region 3. Staff Users
    private static async Task<List<User>> SeedStaffUsersAsync(TechStoreDbContext context, PasswordHasher<User> hasher)
    {
        var staffDefinitions = new (string Username, string FullName, string Email, string Phone, string RoleNormalized)[]
        {
            ("staff_kho", "Trần Văn Kho (Trưởng Kho Tổng)", "staff_kho@techstore.vn", "0988112233", "STAFFPRODUCT"),
            ("staff_product", "Lê Thị Bích Hằng (Chuyên Viên Sản Phẩm)", "staff_product@techstore.vn", "0988223344", "STAFFPRODUCT"),
            ("staff_order", "Nguyễn Hoàng Đơn (Điều Phối Đơn Hàng)", "staff_order@techstore.vn", "0988334455", "STAFFORDER"),
            ("staff_ship", "Võ Minh Giao (Chuyên Viên Vận Chuyển)", "staff_ship@techstore.vn", "0988445566", "STAFFORDER"),
            ("staff_marketing", "Đặng Thu Trang (Trưởng Phòng Marketing)", "staff_marketing@techstore.vn", "0988556677", "STAFFMARKETING"),
            ("staff_cskh", "Phạm Mai Linh (Chăm Sóc Khách Hàng)", "staff_cskh@techstore.vn", "0988667788", "STAFFMARKETING")
        };

        var resultStaff = new List<User>();

        foreach (var (uname, fname, email, phone, roleNorm) in staffDefinitions)
        {
            var user = await context.Users.FirstOrDefaultAsync(u => u.NormalizedUsername == uname.ToUpperInvariant());
            if (user == null)
            {
                user = new User
                {
                    Username = uname,
                    NormalizedUsername = uname.ToUpperInvariant(),
                    FullName = fname,
                    Email = email,
                    NormalizedEmail = email.ToUpperInvariant(),
                    PhoneNumber = phone,
                    EmailConfirmed = true,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow.AddMonths(-6)
                };
                string defaultPwd = uname.Contains("marketing") ? "Marketing@123" : "Staff@123";
                user.PasswordHash = hasher.HashPassword(user, defaultPwd);
                context.Users.Add(user);
                await context.SaveChangesAsync();

                var role = await context.Roles.FirstOrDefaultAsync(r => r.NormalizedName == roleNorm);
                if (role != null)
                {
                    context.UserRoles.Add(new UserRole
                    {
                        UserId = user.UserId,
                        RoleId = role.RoleId
                    });
                    await context.SaveChangesAsync();
                }
            }
            resultStaff.Add(user);
        }

        return resultStaff;
    }
    #endregion

    #region 4. Customers & Customer Addresses
    private static async Task<List<User>> SeedCustomersAndAddressesAsync(TechStoreDbContext context, PasswordHasher<User> hasher)
    {
        var customerData = new (string Username, string FullName, string Email, string Phone, string Province, string District, string Ward, string Street, string? StreetAlt)[]
        {
            ("hoangnam_tech", "Hoàng Văn Nam", "hoangnam.tech@gmail.com", "0912345678", "Hà Nội", "Cầu Giấy", "Dịch Vọng", "Số 26 Ngõ 165 Cầu Giấy", "Tòa nhà FPT Tower, Phố Duy Tân"),
            ("thuyduong_apple", "Trần Thùy Dương", "thuyduong.design@gmail.com", "0934567890", "TP. Hồ Chí Minh", "Quận 3", "Võ Thị Sáu", "182B Nam Kỳ Khởi Nghĩa", "68 Trương Định"),
            ("quocbao_gaming", "Đặng Quốc Bảo", "quocbao.pro@gmail.com", "0905123987", "Đà Nẵng", "Hải Châu", "Hải Châu 1", "72 Bạch Đằng", "154 Nguyễn Văn Linh"),
            ("mailinh_le", "Lê Thị Mai Linh", "mailinh.photo@gmail.com", "0987654321", "Cần Thơ", "Ninh Kiều", "An Khánh", "89 Nguyễn Văn Cừ", null),
            ("dinhtrong_audio", "Vũ Đình Trọng", "dinhtrong.sound@gmail.com", "0933887766", "Hải Phòng", "Lê Chân", "Trần Nguyên Hãn", "246 Tôn Đức Thắng", null),
            ("ngocanh_finance", "Lê Thị Ngọc Anh", "ngocanh.cfa@gmail.com", "0368979222", "Hà Nội", "Ba Đình", "Liễu Giai", "Tòa nhà Lotte Center, 54 Liễu Giai", "28 Đội Cấn"),
            ("minhtuan_dev", "Bùi Minh Tuấn", "minhtuan.dev@gmail.com", "0977112233", "Bình Dương", "Thủ Dầu Một", "Phú Hòa", "12 Đại Lộ Bình Dương", null),
            ("thanhhuong_pr", "Nguyễn Thanh Hương", "thanhhuong.pr@gmail.com", "0966223344", "TP. Hồ Chí Minh", "Bình Thạnh", "Phường 25", "Tòa nhà Landmark 81, Vinhomes Central Park", null),
            ("vanthanh_arch", "Đỗ Văn Thành", "vanthanh.arch@gmail.com", "0918334455", "Khánh Hòa", "Nha Trang", "Lộc Thọ", "42 Trần Phú", null),
            ("kimngan_dr", "Bác sĩ Trần Kim Ngân", "kimngan.med@gmail.com", "0908445566", "TP. Hồ Chí Minh", "Quận 10", "Phường 12", "201 Cao Thắng Nối Dài", null),
            ("ducmanh_ceo", "Phan Đức Mạnh", "ducmanh.ceo@gmail.com", "0938556677", "Đà Nẵng", "Sơn Trà", "An Hải Bắc", "05 Phạm Văn Đồng", null),
            ("phuongthao_mkt", "Vũ Phương Thảo", "phuongthao.lead@gmail.com", "0989667788", "Hà Nội", "Đống Đa", "Láng Hạ", "102 Thái Thịnh", null),
            ("trongnhan_vlog", "Huỳnh Trọng Nhân", "trongnhan.media@gmail.com", "0903778899", "Bà Rịa - Vũng Tàu", "Vũng Tàu", "Phường 2", "15 Thùy Vân", null),
            ("anhthu_law", "Luật sư Nguyễn Anh Thư", "anhthu.lawyer@gmail.com", "0919889900", "Cần Thơ", "Cái Răng", "Lê Bình", "75 Đường 30 Tháng 4", null),
            ("tuananh_bank", "Trương Tuấn Anh", "tuananh.bank@gmail.com", "0978990011", "Hà Nội", "Hai Bà Trưng", "Bạch Đằng", "191 Bà Triệu", null),
            ("huyenmy_style", "Đặng Huyền My", "huyenmy.style@gmail.com", "0932113355", "TP. Hồ Chí Minh", "Quận 7", "Tân Phong", "Tòa nhà Crescent Plaza, Phú Mỹ Hưng", null)
        };

        var customerRole = await context.Roles.FirstOrDefaultAsync(r => r.NormalizedName == "CUSTOMER")
                           ?? await context.Roles.FirstOrDefaultAsync(r => r.RoleId == 5);

        var resultCustomers = new List<User>();

        // Thêm cả vipcustomer nếu có
        var vip = await context.Users.FirstOrDefaultAsync(u => u.NormalizedUsername == "VIPCUSTOMER");
        if (vip != null) resultCustomers.Add(vip);

        int daysAgo = 180;
        foreach (var c in customerData)
        {
            var user = await context.Users.FirstOrDefaultAsync(u => u.NormalizedUsername == c.Username.ToUpperInvariant());
            if (user == null)
            {
                user = new User
                {
                    Username = c.Username,
                    NormalizedUsername = c.Username.ToUpperInvariant(),
                    FullName = c.FullName,
                    Email = c.Email,
                    NormalizedEmail = c.Email.ToUpperInvariant(),
                    PhoneNumber = c.Phone,
                    EmailConfirmed = true,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-daysAgo)
                };
                user.PasswordHash = hasher.HashPassword(user, "Customer@123");
                context.Users.Add(user);
                await context.SaveChangesAsync();

                if (customerRole != null)
                {
                    context.UserRoles.Add(new UserRole
                    {
                        UserId = user.UserId,
                        RoleId = customerRole.RoleId
                    });
                }

                // Địa chỉ mặc định
                context.CustomerAddresses.Add(new CustomerAddress
                {
                    UserId = user.UserId,
                    RecipientName = c.FullName,
                    PhoneNumber = c.Phone,
                    Province = c.Province,
                    District = c.District,
                    Ward = c.Ward,
                    StreetAddress = c.Street,
                    IsDefault = true
                });

                // Địa chỉ phụ (nếu có)
                if (!string.IsNullOrEmpty(c.StreetAlt))
                {
                    context.CustomerAddresses.Add(new CustomerAddress
                    {
                        UserId = user.UserId,
                        RecipientName = c.FullName + " (Văn Phòng)",
                        PhoneNumber = c.Phone,
                        Province = c.Province,
                        District = c.District,
                        Ward = c.Ward,
                        StreetAddress = c.StreetAlt,
                        IsDefault = false
                    });
                }
                await context.SaveChangesAsync();
            }
            resultCustomers.Add(user);
            daysAgo -= 10;
        }

        return resultCustomers;
    }
    #endregion

    #region 5. New Products, Variants, Images & Specs
    private static async Task SeedNewProductsAsync(TechStoreDbContext context)
    {
        var catPhones = await context.Categories.FirstOrDefaultAsync(c => c.Slug == "dien-thoai");
        var catLaptops = await context.Categories.FirstOrDefaultAsync(c => c.Slug == "laptop");
        var catTablets = await context.Categories.FirstOrDefaultAsync(c => c.Slug == "tablet");
        var catAudio = await context.Categories.FirstOrDefaultAsync(c => c.Slug == "am-thanh");
        var catDisplays = await context.Categories.FirstOrDefaultAsync(c => c.Slug == "tivi-man-hinh");

        var brandApple = await context.Brands.FirstOrDefaultAsync(b => b.Slug == "apple");
        var brandSamsung = await context.Brands.FirstOrDefaultAsync(b => b.Slug == "samsung");
        var brandDell = await context.Brands.FirstOrDefaultAsync(b => b.Slug == "dell");
        var brandAsus = await context.Brands.FirstOrDefaultAsync(b => b.Slug == "asus");
        var brandSony = await context.Brands.FirstOrDefaultAsync(b => b.Slug == "sony");
        var brandXiaomi = await context.Brands.FirstOrDefaultAsync(b => b.Slug == "xiaomi");
        var brandLg = await context.Brands.FirstOrDefaultAsync(b => b.Slug == "lg");
        var brandLenovo = await context.Brands.FirstOrDefaultAsync(b => b.Slug == "lenovo");
        var brandMarshall = await context.Brands.FirstOrDefaultAsync(b => b.Slug == "marshall");
        var brandJbl = await context.Brands.FirstOrDefaultAsync(b => b.Slug == "jbl");
        var brandGoogle = await context.Brands.FirstOrDefaultAsync(b => b.Slug == "google") ?? brandXiaomi;
        var brandHp = await context.Brands.FirstOrDefaultAsync(b => b.Slug == "hp") ?? brandDell;
        var brandOppo = await context.Brands.FirstOrDefaultAsync(b => b.Slug == "oppo") ?? brandXiaomi;
        var brandAcer = await context.Brands.FirstOrDefaultAsync(b => b.Slug == "acer") ?? brandAsus;

        var supApple = await context.Suppliers.FirstOrDefaultAsync(s => s.Name.Contains("Apple"));
        var supSamsung = await context.Suppliers.FirstOrDefaultAsync(s => s.Name.Contains("Samsung"));
        var supFpt = await context.Suppliers.FirstOrDefaultAsync(s => s.Name.Contains("FPT") || s.Name.Contains("Synnex"));
        var supAsus = await context.Suppliers.FirstOrDefaultAsync(s => s.Name.Contains("ASUS"));
        var supDgw = await context.Suppliers.FirstOrDefaultAsync(s => s.Name.Contains("Digiworld"));
        var supAsh = await context.Suppliers.FirstOrDefaultAsync(s => s.Name.Contains("Ash") || s.Name.Contains("Marshall"));
        var supPgi = await context.Suppliers.FirstOrDefaultAsync(s => s.Name.Contains("Phúc Giang") || s.Name.Contains("PGI"));
        var supSpc = await context.Suppliers.FirstOrDefaultAsync(s => s.Name.Contains("Vĩnh Xuân") || s.Name.Contains("SPC"));
        var supLg = await context.Suppliers.FirstOrDefaultAsync(s => s.Name.Contains("LG"));
        var supSony = await context.Suppliers.FirstOrDefaultAsync(s => s.Name.Contains("Sony"));

        var productsToCreate = new List<(
            string Name, string Slug, int CatId, int BrandId, int? SupId,
            string FeaturedImg, string ShortDesc, string FullDesc, int Warranty, bool IsFeatured,
            Dictionary<string, string> Specs,
            List<(string Sku, string VarName, decimal OrigPrice, decimal SalePrice, int Stock, string Img)> Variants,
            List<string> GalleryImgs
        )>
        {
            // 1. Samsung Galaxy Z Flip6
            (
                "Samsung Galaxy Z Flip6 5G (Màn Hình Phụ Flex Window)",
                "samsung-galaxy-z-flip6",
                catPhones!.CategoryId, brandSamsung!.BrandId, supSamsung?.SupplierId,
                "https://images.unsplash.com/photo-1592899677977-9c10ca588bbd?auto=format&fit=crop&w=800&q=80",
                "Thiết kế gập vỏ sò thời thượng, camera 50MP AI đột phá cùng bản lề FlexHinge không nếp gấp.",
                "Samsung Galaxy Z Flip6 là biểu tượng công nghệ gập thời thượng năm 2026. Trang bị bộ vi xử lý Snapdragon 8 Gen 3 for Galaxy, buồng tản nhiệt hơi Vapor Chamber đầu tiên trên dòng Flip, pin 4.000 mAh cùng loạt tính năng Galaxy AI thông minh vượt bậc.",
                12, true,
                new Dictionary<string, string>
                {
                    ["Màn hình chính"] = "6.7 inch Dynamic AMOLED 2X, 120Hz thích ứng, 2600 nits",
                    ["Màn hình phụ"] = "3.4 inch Super AMOLED 60Hz Flex Window tiện lợi",
                    ["Vi xử lý"] = "Snapdragon 8 Gen 3 for Galaxy (4nm)",
                    ["RAM"] = "12GB LPDDR5X",
                    ["Camera sau"] = "Chính 50MP Dual Pixel OIS + Siêu rộng 12MP",
                    ["Pin & Sạc"] = "4.000 mAh, Sạc nhanh 25W, Sạc không dây Fast Wireless 2.0",
                    ["Độ bền"] = "Kháng nước IP48, Khung viền Armor Aluminum cứng cáp"
                },
                new List<(string, string, decimal, decimal, int, string)>
                {
                    ("SS-ZFLIP6-256-BLU", "Galaxy Z Flip6 5G 256GB Xanh Maya", 28990000m, 25990000m, 35, "https://images.unsplash.com/photo-1592899677977-9c10ca588bbd?auto=format&fit=crop&w=800&q=80"),
                    ("SS-ZFLIP6-256-YEL", "Galaxy Z Flip6 5G 256GB Vàng Nắng", 28990000m, 25990000m, 28, "https://images.unsplash.com/photo-1580910051074-3eb694886505?auto=format&fit=crop&w=800&q=80"),
                    ("SS-ZFLIP6-512-GRY", "Galaxy Z Flip6 5G 512GB Xám Titan", 32990000m, 29990000m, 20, "https://images.unsplash.com/photo-1610945415295-d9bbf067e59c?auto=format&fit=crop&w=800&q=80")
                },
                new List<string>
                {
                    "https://images.unsplash.com/photo-1580910051074-3eb694886505?auto=format&fit=crop&w=800&q=80",
                    "https://images.unsplash.com/photo-1610945415295-d9bbf067e59c?auto=format&fit=crop&w=800&q=80"
                }
            ),

            // 2. Google Pixel 9 Pro XL
            (
                "Google Pixel 9 Pro XL 5G AI Tensor G4",
                "google-pixel-9-pro-xl",
                catPhones!.CategoryId, (brandGoogle ?? brandXiaomi)!.BrandId, supDgw?.SupplierId,
                "https://images.unsplash.com/photo-1598327105666-5b89351aff97?auto=format&fit=crop&w=800&q=80",
                "Trải nghiệm AI thuần khiết từ Google với cụm camera 50MP Super Res Zoom 30x và màn hình 3000 nits đỉnh cao.",
                "Google Pixel 9 Pro XL sở hữu thiết kế thanh camera iconic được tái định nghĩa, vi xử lý Google Tensor G4 tối ưu riêng cho mô hình ngôn ngữ lớn Gemini Nano. Cảm biến nhiệt độ, thời gian cập nhật phần mềm cam kết lên đến 7 năm.",
                12, true,
                new Dictionary<string, string>
                {
                    ["Màn hình"] = "6.8 inch Super Actua OLED, 1-120Hz LTPO, 3000 nits HDR",
                    ["Vi xử lý"] = "Google Tensor G4 (4nm) kèm chip bảo mật Titan M2",
                    ["RAM"] = "16GB RAM chuẩn AI",
                    ["Camera sau"] = "50MP Wide Octa PD + 48MP Ultrawide Macro + 48MP Telephoto 5x",
                    ["Camera trước"] = "42MP Dual PD siêu sắc nét",
                    ["Pin & Sạc"] = "5.060 mAh, Sạc nhanh 37W, Sạc không dây Qi2",
                    ["Hệ điều hành"] = "Android 15 gốc mượt mà không ứng dụng rác"
                },
                new List<(string, string, decimal, decimal, int, string)>
                {
                    ("PIX9PXL-128-OBS", "Google Pixel 9 Pro XL 128GB Obsidian Đen", 30990000m, 27990000m, 22, "https://images.unsplash.com/photo-1598327105666-5b89351aff97?auto=format&fit=crop&w=800&q=80"),
                    ("PIX9PXL-256-POR", "Google Pixel 9 Pro XL 256GB Porcelain Trắng", 33990000m, 30490000m, 18, "https://images.unsplash.com/photo-1511707171634-5f897ff02aa9?auto=format&fit=crop&w=800&q=80"),
                    ("PIX9PXL-512-HAZ", "Google Pixel 9 Pro XL 512GB Hazel Xám Khói", 37990000m, 34990000m, 12, "https://images.unsplash.com/photo-1565849904461-04a58ad377e0?auto=format&fit=crop&w=800&q=80")
                },
                new List<string>
                {
                    "https://images.unsplash.com/photo-1511707171634-5f897ff02aa9?auto=format&fit=crop&w=800&q=80",
                    "https://images.unsplash.com/photo-1565849904461-04a58ad377e0?auto=format&fit=crop&w=800&q=80"
                }
            ),

            // 3. iPhone 15 Pro Max
            (
                "iPhone 15 Pro Max 256GB Chính Hãng VN/A",
                "iphone-15-pro-max",
                catPhones!.CategoryId, brandApple!.BrandId, supApple?.SupplierId,
                "https://images.unsplash.com/photo-1695048133142-1a20484d2569?auto=format&fit=crop&w=800&q=80",
                "Siêu phẩm Titanium nguyên khối, chip Apple A17 Pro 3nm mạnh mẽ cùng camera Telephoto 5x chuyên nghiệp.",
                "iPhone 15 Pro Max là chiếc điện thoại bán chạy nhất toàn cầu với khung viền Titan chuẩn hàng không vũ trụ, cổng kết nối USB-C tốc độ 10Gbps và nút Action Button đa năng tùy biến linh hoạt.",
                12, false,
                new Dictionary<string, string>
                {
                    ["Màn hình"] = "6.7 inch Super Retina XDR OLED, 120Hz ProMotion, Always-On Display",
                    ["Vi xử lý"] = "Apple A17 Pro (3nm, 6-core CPU, 6-core GPU)",
                    ["RAM"] = "8GB",
                    ["Bộ nhớ"] = "256GB / 512GB",
                    ["Camera sau"] = "48MP Main + 12MP Ultra Wide + 12MP Telephoto 5x Tetraprism",
                    ["Pin & Sạc"] = "4.422 mAh, Sạc nhanh 20W, MagSafe 15W",
                    ["Khung viền"] = "Titanium Grade 5 siêu nhẹ bền bỉ"
                },
                new List<(string, string, decimal, decimal, int, string)>
                {
                    ("IP15PM-256-NAT", "iPhone 15 Pro Max 256GB Titan Tự Nhiên", 31990000m, 28990000m, 45, "https://images.unsplash.com/photo-1695048133142-1a20484d2569?auto=format&fit=crop&w=800&q=80"),
                    ("IP15PM-256-BLU", "iPhone 15 Pro Max 256GB Titan Xanh", 31990000m, 28490000m, 30, "https://images.unsplash.com/photo-1510557880182-3d4d3cba35a5?auto=format&fit=crop&w=800&q=80"),
                    ("IP15PM-512-BLK", "iPhone 15 Pro Max 512GB Titan Đen", 36990000m, 33490000m, 15, "https://images.unsplash.com/photo-1530319067432-f2a729c03db5?auto=format&fit=crop&w=800&q=80")
                },
                new List<string>
                {
                    "https://images.unsplash.com/photo-1510557880182-3d4d3cba35a5?auto=format&fit=crop&w=800&q=80",
                    "https://images.unsplash.com/photo-1530319067432-f2a729c03db5?auto=format&fit=crop&w=800&q=80"
                }
            ),

            // 4. Xiaomi Redmi Note 14 Pro+
            (
                "Xiaomi Redmi Note 14 Pro+ 5G (Camera 200MP OIS)",
                "xiaomi-redmi-note-14-pro-plus",
                catPhones!.CategoryId, brandXiaomi!.BrandId, supDgw?.SupplierId,
                "https://images.unsplash.com/photo-1567581935884-3349723552ca?auto=format&fit=crop&w=800&q=80",
                "Màn hình cong 1.5K AMOLED 120Hz, camera 200MP siêu nét và viên pin khủng 6200mAh sạc siêu nhanh 90W.",
                "Redmi Note 14 Pro+ thiết lập chuẩn mực mới cho phân khúc tầm trung với độ bền chuẩn chống nước IP68/IP69K, vi xử lý Snapdragon 7s Gen 3 mượt mà và màn hình bảo vệ mắt chuyên sâu.",
                18, false,
                new Dictionary<string, string>
                {
                    ["Màn hình"] = "6.67 inch AMOLED 1.5K 120Hz cong tràn viền, 3000 nits",
                    ["Vi xử lý"] = "Qualcomm Snapdragon 7s Gen 3 (4nm)",
                    ["RAM"] = "12GB LPDDR4X",
                    ["Camera"] = "200MP Light Hunter 800 OIS + 8MP Góc rộng + 2MP Macro",
                    ["Pin & Sạc"] = "6.200 mAh Silicon-Carbon, Sạc HyperCharge 90W",
                    ["Kháng nước"] = "IP68 & IP69K chịu được tia nước nóng áp lực cao"
                },
                new List<(string, string, decimal, decimal, int, string)>
                {
                    ("RN14P-256-BLU", "Redmi Note 14 Pro+ 5G 256GB Xanh Băng Tuyết", 10490000m, 9490000m, 50, "https://images.unsplash.com/photo-1567581935884-3349723552ca?auto=format&fit=crop&w=800&q=80"),
                    ("RN14P-512-BLK", "Redmi Note 14 Pro+ 5G 512GB Đen Bóng Đêm", 11990000m, 10790000m, 35, "https://images.unsplash.com/photo-1511707171634-5f897ff02aa9?auto=format&fit=crop&w=800&q=80")
                },
                new List<string>
                {
                    "https://images.unsplash.com/photo-1511707171634-5f897ff02aa9?auto=format&fit=crop&w=800&q=80"
                }
            ),

            // 5. Laptop ASUS Zenbook 14 OLED UX3405
            (
                "Laptop ASUS Zenbook 14 OLED UX3405 AI Intel Core Ultra 7",
                "asus-zenbook-14-oled-ux3405",
                catLaptops!.CategoryId, brandAsus!.BrandId, supAsus?.SupplierId,
                "https://images.unsplash.com/photo-1496181133206-80ce9b88a853?auto=format&fit=crop&w=800&q=80",
                "Tuyệt tác mỏng nhẹ 1.2kg vỏ nhôm nguyên khối, chip AI Core Ultra 7 và màn hình 3K Lumina OLED 120Hz.",
                "ASUS Zenbook 14 OLED UX3405 định nghĩa lại laptop siêu di động với thời lượng pin thực tế 15 giờ nhờ viên pin 75Wh, bàn phím ErgoSense êm ái tích hợp NumberPad thông minh trên Touchpad phủ kính.",
                24, true,
                new Dictionary<string, string>
                {
                    ["Màn hình"] = "14 inch 3K (2880 x 1800) ASUS Lumina OLED 120Hz 100% DCI-P3 600 nits",
                    ["Vi xử lý"] = "Intel Core Ultra 7 155H (16 nhân, 22 luồng, tích hợp Intel AI Boost NPU)",
                    ["RAM"] = "16GB / 32GB LPDDR5X 7467MHz",
                    ["Ổ cứng"] = "1TB SSD NVMe M.2 PCIe 4.0",
                    ["Đồ họa"] = "Intel Arc Graphics hiệu năng cao",
                    ["Pin & Trọng lượng"] = "75Wh (dùng 15h liên tục), chỉ nặng 1.2kg, dày 14.9mm",
                    ["Cổng kết nối"] = "2x Thunderbolt 4, 1x USB 3.2 Gen 1 Type-A, 1x HDMI 2.1, Jack 3.5mm"
                },
                new List<(string, string, decimal, decimal, int, string)>
                {
                    ("ASUS-ZEN14-U7-BLU", "Zenbook 14 OLED UX3405 Ultra 7/16GB/1TB Xanh Ponder", 34990000m, 31990000m, 25, "https://images.unsplash.com/photo-1496181133206-80ce9b88a853?auto=format&fit=crop&w=800&q=80"),
                    ("ASUS-ZEN14-U7-32G", "Zenbook 14 OLED UX3405 Ultra 7/32GB/1TB Bạc Foggy", 38990000m, 35490000m, 15, "https://images.unsplash.com/photo-1541807084-5c52b6b3adef?auto=format&fit=crop&w=800&q=80")
                },
                new List<string>
                {
                    "https://images.unsplash.com/photo-1541807084-5c52b6b3adef?auto=format&fit=crop&w=800&q=80"
                }
            ),

            // 6. Lenovo ThinkPad X1 Carbon Gen 12
            (
                "Lenovo ThinkPad X1 Carbon Gen 12 Doanh Nhân Đỉnh Cao",
                "lenovo-thinkpad-x1-carbon-gen12",
                catLaptops!.CategoryId, brandLenovo!.BrandId, supFpt?.SupplierId,
                "https://images.unsplash.com/photo-1588872657578-7efd1f1555ed?auto=format&fit=crop&w=800&q=80",
                "Huyền thoại laptop doanh nghiệp với sợi carbon siêu bền chuẩn quân sự, màn hình 2.8K OLED và bàn phím tốt nhất thế giới.",
                "ThinkPad X1 Carbon Gen 12 sở hữu thiết kế Communications Bar mới với webcam 8MP AI, Touchpad xúc giác Haptic và trackpoint đỏ huyền thoại, hỗ trợ chuẩn Intel vPro bảo mật doanh nghiệp.",
                36, true,
                new Dictionary<string, string>
                {
                    ["Màn hình"] = "14 inch 2.8K (2880 x 1800) OLED 120Hz HDR 500 True Black 100% DCI-P3",
                    ["Vi xử lý"] = "Intel Core Ultra 7 165U vPro (12 nhân, 14 luồng)",
                    ["RAM"] = "32GB LPDDR5X",
                    ["Ổ cứng SSD"] = "1TB SSD NVMe M.2 PCIe Gen 4 Performance",
                    ["Bảo mật"] = "Cảm biến vân tay trên bàn phím, Nhận diện khuôn mặt IR Camera, Chip dTPM 2.0",
                    ["Chất liệu & Cân nặng"] = "Hợp kim Magie & Sợi Carbon gia cường, chỉ nặng 1.09kg",
                    ["Độ bền"] = "Chứng nhận độ bền tiêu chuẩn quân đội Mỹ MIL-STD-810H"
                },
                new List<(string, string, decimal, decimal, int, string)>
                {
                    ("TP-X1CG12-32G-1T", "ThinkPad X1 Carbon Gen 12 Ultra 7/32GB/1TB Đen Carbon", 54990000m, 49990000m, 12, "https://images.unsplash.com/photo-1588872657578-7efd1f1555ed?auto=format&fit=crop&w=800&q=80")
                },
                new List<string>
                {
                    "https://images.unsplash.com/photo-1517336714731-489689fd1ca8?auto=format&fit=crop&w=800&q=80"
                }
            ),

            // 7. Dell XPS 14 9440 OLED
            (
                "Dell XPS 14 9440 Intel Core Ultra 7 RTX 4050 3.2K OLED",
                "dell-xps-14-9440",
                catLaptops!.CategoryId, brandDell!.BrandId, supSpc?.SupplierId,
                "https://images.unsplash.com/photo-1593642632823-8f785ba67e45?auto=format&fit=crop&w=800&q=80",
                "Kiệt tác thiết kế nguyên khối nhôm CNC cắt vát kim cương, bàn di chuột kính vô hình và đồ họa rời RTX 4050 mạnh mẽ.",
                "Dell XPS 14 9440 mang phong cách tối giản tương lai với dải phím cảm ứng Function điện dung, màn hình vô cực InfinityEdge OLED cảm ứng và âm thanh 4 loa vòm công nghệ Waves MaxxAudio Pro.",
                24, false,
                new Dictionary<string, string>
                {
                    ["Màn hình"] = "14.5 inch 3.2K (3200 x 2000) OLED Touch 120Hz 100% DCI-P3 kính Gorilla Glass 3",
                    ["Vi xử lý"] = "Intel Core Ultra 7 155H (16 nhân, 22 luồng, up to 4.8GHz)",
                    ["Card đồ họa"] = "NVIDIA GeForce RTX 4050 6GB GDDR6 (30W)",
                    ["RAM"] = "32GB LPDDR5X 7467MHz",
                    ["Ổ cứng SSD"] = "1TB M.2 PCIe NVMe SSD",
                    ["Trọng lượng"] = "1.74 kg",
                    ["Kết nối"] = "3x Thunderbolt 4 hỗ trợ sạc DisplayPort, khe thẻ nhớ microSD, Jack 3.5mm"
                },
                new List<(string, string, decimal, decimal, int, string)>
                {
                    ("DELL-XPS14-32G-SLV", "Dell XPS 14 9440 Ultra 7/32GB/1TB/RTX4050 Bạc Platinum", 62990000m, 57990000m, 10, "https://images.unsplash.com/photo-1593642632823-8f785ba67e45?auto=format&fit=crop&w=800&q=80")
                },
                new List<string>
                {
                    "https://images.unsplash.com/photo-1588872657578-7efd1f1555ed?auto=format&fit=crop&w=800&q=80"
                }
            ),

            // 8. iPad Air 11 inch M2
            (
                "iPad Air 11 inch M2 Wi-Fi Chính Hãng Apple VN/A",
                "ipad-air-11-m2",
                catTablets!.CategoryId, brandApple!.BrandId, supApple?.SupplierId,
                "https://images.unsplash.com/photo-1544244015-0df4b3ffc6b0?auto=format&fit=crop&w=800&q=80",
                "Hiệu năng bứt phá với vi xử lý Apple M2, hỗ trợ bút Apple Pencil Pro mới và camera trước ngang thông minh.",
                "iPad Air 11 inch M2 đem đến sức mạnh không tưởng cho thiết kế đồ họa, dựng video 4K và chơi game đồ họa đỉnh cao với mức giá vô cùng dễ tiếp cận.",
                12, true,
                new Dictionary<string, string>
                {
                    ["Màn hình"] = "11 inch Liquid Retina LED IPS 500 nits, Dải màu rộng P3, True Tone",
                    ["Vi xử lý"] = "Apple M2 (8-core CPU, 9-core GPU, 16-core Neural Engine)",
                    ["RAM"] = "8GB Unified Memory",
                    ["Bộ nhớ"] = "128GB / 256GB NVMe",
                    ["Phụ kiện tương thích"] = "Hỗ trợ Apple Pencil Pro, Apple Pencil (USB-C), Magic Keyboard",
                    ["Camera"] = "Sau 12MP Wide quay 4K, Trước 12MP Ultra Wide góc rộng Center Stage"
                },
                new List<(string, string, decimal, decimal, int, string)>
                {
                    ("IPADAIR-11-128-BLU", "iPad Air 11 M2 Wi-Fi 128GB Xanh Dương", 16990000m, 15490000m, 30, "https://images.unsplash.com/photo-1544244015-0df4b3ffc6b0?auto=format&fit=crop&w=800&q=80"),
                    ("IPADAIR-11-128-PRP", "iPad Air 11 M2 Wi-Fi 128GB Tím Pastel", 16990000m, 15490000m, 25, "https://images.unsplash.com/photo-1561154464-82e9adf32764?auto=format&fit=crop&w=800&q=80"),
                    ("IPADAIR-11-256-STA", "iPad Air 11 M2 Wi-Fi 256GB Ánh Sao", 19990000m, 18190000m, 20, "https://images.unsplash.com/photo-1585790050230-5dd28404ccb9?auto=format&fit=crop&w=800&q=80")
                },
                new List<string>
                {
                    "https://images.unsplash.com/photo-1561154464-82e9adf32764?auto=format&fit=crop&w=800&q=80",
                    "https://images.unsplash.com/photo-1585790050230-5dd28404ccb9?auto=format&fit=crop&w=800&q=80"
                }
            ),

            // 9. iPad mini 7 A17 Pro
            (
                "iPad mini 7 8.3 inch Chip A17 Pro Apple Intelligence",
                "ipad-mini-7-a17-pro",
                catTablets!.CategoryId, brandApple!.BrandId, supApple?.SupplierId,
                "https://images.unsplash.com/photo-1585790050230-5dd28404ccb9?auto=format&fit=crop&w=800&q=80",
                "Kích thước siêu nhỏ gọn vừa lòng bàn tay, sức mạnh khủng khiếp từ chip A17 Pro hỗ trợ trọn vẹn Apple Intelligence.",
                "iPad mini thế hệ 7 mang đến tốc độ kết nối Wi-Fi 6E siêu nhanh, cổng USB-C tốc độ gấp đôi 10Gbps và hỗ trợ chuẩn sạc bút Apple Pencil Pro mới nhất.",
                12, false,
                new Dictionary<string, string>
                {
                    ["Màn hình"] = "8.3 inch Liquid Retina 500 nits chống lóa, True Tone, P3",
                    ["Vi xử lý"] = "Apple A17 Pro (6-core CPU, 5-core GPU, Hardware Ray Tracing)",
                    ["RAM"] = "8GB RAM tối ưu cho Apple Intelligence",
                    ["Bộ nhớ"] = "128GB / 256GB",
                    ["Trọng lượng"] = "Chỉ 293 g cực kỳ tiện lợi mang theo hàng ngày",
                    ["Kết nối"] = "Wi-Fi 6E (802.11ax), Bluetooth 5.3, USB-C 10Gb/s"
                },
                new List<(string, string, decimal, decimal, int, string)>
                {
                    ("IPADMINI7-128-GRY", "iPad mini 7 128GB Xám Không Gian", 14990000m, 13790000m, 25, "https://images.unsplash.com/photo-1585790050230-5dd28404ccb9?auto=format&fit=crop&w=800&q=80"),
                    ("IPADMINI7-256-BLU", "iPad mini 7 256GB Xanh Ánh Kim", 17990000m, 16290000m, 18, "https://images.unsplash.com/photo-1544244015-0df4b3ffc6b0?auto=format&fit=crop&w=800&q=80")
                },
                new List<string>
                {
                    "https://images.unsplash.com/photo-1544244015-0df4b3ffc6b0?auto=format&fit=crop&w=800&q=80"
                }
            ),

            // 10. Samsung Galaxy Tab S9 FE
            (
                "Samsung Galaxy Tab S9 FE 5G Kèm Bút S-Pen Kháng Nước IP68",
                "samsung-galaxy-tab-s9-fe",
                catTablets!.CategoryId, brandSamsung!.BrandId, supSamsung?.SupplierId,
                "https://images.unsplash.com/photo-1561154464-82e9adf32764?auto=format&fit=crop&w=800&q=80",
                "Máy tính bảng bền bỉ kháng nước IP68 đầu tiên trong phân khúc, kèm sẵn bút S-Pen độ trễ thấp và màn hình 90Hz mượt mà.",
                "Galaxy Tab S9 FE 5G là người bạn đồng hành hoàn hảo cho học sinh, sinh viên và dân văn phòng với tính năng Samsung DeX biến máy tính bảng thành giao diện PC đa nhiệm tiện lợi.",
                12, false,
                new Dictionary<string, string>
                {
                    ["Màn hình"] = "10.9 inch WQXGA (2304 x 1440) 90Hz mượt mà, công nghệ Vision Booster",
                    ["Vi xử lý"] = "Exynos 1380 (5nm) 8 nhân đa nhiệm mạnh mẽ",
                    ["RAM & Bộ nhớ"] = "6GB RAM / 128GB (Hỗ trợ thẻ nhớ MicroSD tới 1TB)",
                    ["Kháng nước & Bụi"] = "Chuẩn IP68 cho cả thân máy lẫn bút cảm ứng S-Pen",
                    ["Pin & Sạc"] = "8.000 mAh dùng cả ngày, sạc nhanh 45W",
                    ["Âm thanh"] = "Loa kép AKG hỗ trợ âm thanh vòm Dolby Atmos sống động"
                },
                new List<(string, string, decimal, decimal, int, string)>
                {
                    ("TAB-S9FE-128-GRN", "Galaxy Tab S9 FE 5G 128GB Xanh Nhiệt Đới", 11990000m, 10490000m, 20, "https://images.unsplash.com/photo-1561154464-82e9adf32764?auto=format&fit=crop&w=800&q=80"),
                    ("TAB-S9FE-128-GRY", "Galaxy Tab S9 FE 5G 128GB Xám Tinh Tế", 11990000m, 10490000m, 25, "https://images.unsplash.com/photo-1585790050230-5dd28404ccb9?auto=format&fit=crop&w=800&q=80")
                },
                new List<string>
                {
                    "https://images.unsplash.com/photo-1585790050230-5dd28404ccb9?auto=format&fit=crop&w=800&q=80"
                }
            ),

            // 11. Marshall Emberton II
            (
                "Loa Bluetooth Di Động Marshall Emberton II Chính Hãng",
                "marshall-emberton-ii",
                catAudio!.CategoryId, brandMarshall!.BrandId, supAsh?.SupplierId,
                "https://images.unsplash.com/photo-1545454675-3531b543be5d?auto=format&fit=crop&w=800&q=80",
                "Âm thanh đa hướng 360 độ True Stereophonic đặc trưng Marshall, pin 30+ giờ và chuẩn kháng nước bụi IP67 bền bỉ.",
                "Emberton II mang phong cách rock-and-roll cổ điển huyền thoại của Marshall với lớp vỏ họa tiết vân da chống va đập, nút điều khiển đa hướng dạng núm đồng brass sang trọng và chế độ ghép đôi Stack Mode độc đáo.",
                12, true,
                new Dictionary<string, string>
                {
                    ["Công suất"] = "2x 10W Class D Amplifier (Âm thanh nổi 360 độ đa hướng)",
                    ["Dải tần đáp ứng"] = "60 Hz - 20 kHz, Max SPL 87 dB @ 1 m",
                    ["Thời lượng pin"] = "Hơn 30 giờ chơi nhạc liên tục (Sạc đầy trong 3 giờ, sạc nhanh 20 phút dùng 4 giờ)",
                    ["Chuẩn kháng nước"] = "IP67 (Chống bụi hoàn toàn và ngâm nước độ sâu 1 mét trong 30 phút)",
                    ["Kết nối"] = "Bluetooth 5.1 phạm vi 10 mét, App Marshall Bluetooth",
                    ["Trọng lượng"] = "0.7 kg nhỏ gọn dễ cầm tay hoặc bỏ ba lô"
                },
                new List<(string, string, decimal, decimal, int, string)>
                {
                    ("MAR-EMB2-BLK", "Loa Marshall Emberton II Black and Brass (Đen Viền Vàng)", 4490000m, 3990000m, 40, "https://images.unsplash.com/photo-1545454675-3531b543be5d?auto=format&fit=crop&w=800&q=80"),
                    ("MAR-EMB2-CRM", "Loa Marshall Emberton II Cream (Kem Cổ Điển)", 4490000m, 4190000m, 25, "https://images.unsplash.com/photo-1508700115892-45ecd05ae2ad?auto=format&fit=crop&w=800&q=80")
                },
                new List<string>
                {
                    "https://images.unsplash.com/photo-1508700115892-45ecd05ae2ad?auto=format&fit=crop&w=800&q=80"
                }
            ),

            // 12. Apple AirPods Max USB-C
            (
                "Tai Nghe Chụp Tai Apple AirPods Max (Cổng USB-C Mới)",
                "apple-airpods-max-usbc",
                catAudio!.CategoryId, brandApple!.BrandId, supApple?.SupplierId,
                "https://images.unsplash.com/photo-1505740420928-5e560c06d30e?auto=format&fit=crop&w=800&q=80",
                "Chống ồn chủ động đẳng cấp thế giới, âm thanh không gian Spatial Audio theo dõi đầu và cổng sạc USB-C hiện đại.",
                "AirPods Max phiên bản cổng sạc USB-C đem đến sự đồng bộ tiện lợi tuyệt đối với hệ sinh thái Apple hiện đại, đệm tai bọc vải lưới thoáng khí và vòm tai nhôm Anodized sang trọng thượng thừa.",
                12, true,
                new Dictionary<string, string>
                {
                    ["Củ loa"] = "Củ loa Dynamic 40mm do Apple tự thiết kế độc quyền",
                    ["Công nghệ chống ồn"] = "Chống ồn chủ động chuyên nghiệp (ANC) & Chế độ Xuyên âm (Transparency)",
                    ["Âm thanh"] = "Spatial Audio cá nhân hóa với theo dõi chuyển động đầu động cơ học",
                    ["Thời lượng pin"] = "20 giờ nghe nhạc hoặc đàm thoại khi bật chống ồn ANC",
                    ["Cổng kết nối"] = "Cổng sạc chuẩn USB-C mới, sạc nhanh 5 phút dùng 1.5 giờ",
                    ["Chất liệu"] = "Khung thép không gỉ bọc vật liệu êm ái, chụp tai nhôm Anodized bóng bẩy"
                },
                new List<(string, string, decimal, decimal, int, string)>
                {
                    ("APM-USBC-MID", "Apple AirPods Max USB-C Đen Nửa Đêm (Midnight)", 13990000m, 12990000m, 20, "https://images.unsplash.com/photo-1505740420928-5e560c06d30e?auto=format&fit=crop&w=800&q=80"),
                    ("APM-USBC-STA", "Apple AirPods Max USB-C Ánh Sao (Starlight)", 13990000m, 12990000m, 15, "https://images.unsplash.com/photo-1583394838336-acd977736f90?auto=format&fit=crop&w=800&q=80"),
                    ("APM-USBC-BLU", "Apple AirPods Max USB-C Xanh Da Trời (Blue)", 13990000m, 12990000m, 12, "https://images.unsplash.com/photo-1484704849700-f032a568e944?auto=format&fit=crop&w=800&q=80")
                },
                new List<string>
                {
                    "https://images.unsplash.com/photo-1583394838336-acd977736f90?auto=format&fit=crop&w=800&q=80",
                    "https://images.unsplash.com/photo-1484704849700-f032a568e944?auto=format&fit=crop&w=800&q=80"
                }
            ),

            // 13. JBL Boombox 3 Wi-Fi
            (
                "Loa Bluetooth Công Suất Lớn JBL Boombox 3 Wi-Fi & Dolby Atmos",
                "jbl-boombox-3-wifi",
                catAudio!.CategoryId, brandJbl!.BrandId, supPgi?.SupplierId,
                "https://images.unsplash.com/photo-1545454675-3531b543be5d?auto=format&fit=crop&w=800&q=80",
                "Công suất khủng 180W RMS, âm trầm rung chuyển mặt sàn, hỗ trợ truyền phát Wi-Fi âm thanh HD và Dolby Atmos.",
                "JBL Boombox 3 Wi-Fi biến mọi bữa tiệc ngoài trời thành đại nhạc hội sống động với tay cầm kim loại chắc chắn bọc silicon chống trượt, pin 24 giờ tích hợp cổng sạc dự phòng cho điện thoại.",
                12, false,
                new Dictionary<string, string>
                {
                    ["Công suất"] = "1x 80W RMS Subwoofer + 2x 40W Mid + 2x 10W Tweeter (Tổng 180W RMS cắm điện)",
                    ["Âm thanh"] = "JBL Original Pro Sound, hỗ trợ 3D Dolby Atmos qua kết nối Wi-Fi",
                    ["Kết nối"] = "Wi-Fi 6, AirPlay 2, Alexa Multi-Room, Chromecast, Spotify Connect, Bluetooth 5.3",
                    ["Thời lượng pin"] = "24 giờ chơi nhạc liên tục (Pin 9.600 mAh sạc ngược điện thoại)",
                    ["Kháng nước bụi"] = "Chuẩn chống nước và bụi bẩn IP67 an toàn bên hồ bơi, bãi biển"
                },
                new List<(string, string, decimal, decimal, int, string)>
                {
                    ("JBL-BB3-WIFI-BLK", "Loa di động JBL Boombox 3 Wi-Fi Đen Mạnh Mẽ", 14990000m, 12990000m, 18, "https://images.unsplash.com/photo-1545454675-3531b543be5d?auto=format&fit=crop&w=800&q=80")
                },
                new List<string>
                {
                    "https://images.unsplash.com/photo-1508700115892-45ecd05ae2ad?auto=format&fit=crop&w=800&q=80"
                }
            ),

            // 14. Màn hình Gaming Samsung Odyssey OLED G8 34 inch
            (
                "Màn Hình Cong Gaming Samsung Odyssey OLED G8 34 inch UWQHD 175Hz",
                "man-hinh-gaming-samsung-odyssey-oled-g8",
                catDisplays!.CategoryId, brandSamsung!.BrandId, supSamsung?.SupplierId,
                "https://images.unsplash.com/photo-1527443224154-c4a3942d3acf?auto=format&fit=crop&w=800&q=80",
                "Tấm nền QD-OLED cong 1800R tỷ lệ 21:9 siêu rộng, tần số quét 175Hz và thời gian phản hồi siêu tốc 0.03ms.",
                "Samsung Odyssey OLED G8 sở hữu thiết kế kim loại siêu mỏng chỉ 3.9mm ở phần mỏng nhất, dải đèn LED CoreSync phía sau đồng bộ màu sắc trò chơi và bộ xử lý Neo Quantum Processor tối ưu hình ảnh tức thì.",
                36, true,
                new Dictionary<string, string>
                {
                    ["Kích thước & Độ cong"] = "34 inch cong 1800R, Tỷ lệ khung hình 21:9 Ultra-WQHD (3440 x 1440)",
                    ["Tấm nền & Tần số quét"] = "QD-OLED cao cấp, 175Hz chuyển động siêu mượt, Phản hồi 0.03ms (GtG)",
                    ["Màu sắc & Độ sáng"] = "99.3% DCI-P3, DisplayHDR True Black 400, Độ tương phản 1.000.000:1",
                    ["Công nghệ đồng bộ"] = "AMD FreeSync Premium Pro, VESA AdaptiveSync chống xé hình",
                    ["Hệ điều hành tích hợp"] = "Tizen OS Smart TV (xem Netflix, YouTube không cần bật PC)",
                    ["Cổng kết nối"] = "Micro HDMI 2.1, Mini DisplayPort, USB-C hỗ trợ Power Delivery 65W"
                },
                new List<(string, string, decimal, decimal, int, string)>
                {
                    ("SS-ODY-G8-34", "Màn hình Gaming Samsung Odyssey OLED G8 34 inch 175Hz Bạc Titan", 27990000m, 24490000m, 15, "https://images.unsplash.com/photo-1527443224154-c4a3942d3acf?auto=format&fit=crop&w=800&q=80")
                },
                new List<string>
                {
                    "https://images.unsplash.com/photo-1585792180666-f7347c490ee2?auto=format&fit=crop&w=800&q=80"
                }
            ),

            // 15. Smart TV LG OLED evo 55 inch C4
            (
                "Smart TV LG OLED evo 55 inch 4K C4 Cinema 144Hz AI Processor",
                "smart-tv-lg-oled-evo-55-c4",
                catDisplays!.CategoryId, brandLg!.BrandId, supLg?.SupplierId,
                "https://images.unsplash.com/photo-1593784991095-a205069470b6?auto=format&fit=crop&w=800&q=80",
                "Tấm nền OLED evo Brightness Booster tăng sáng vượt bậc, bộ xử lý α9 AI Gen7 cùng trải nghiệm chuẩn rạp chiếu Dolby Vision & Atmos.",
                "LG OLED C4 là chuẩn mực vàng của TV cao cấp với 4 cổng HDMI 2.1 4K 144Hz tương thích NVIDIA G-Sync, hệ điều hành webOS 24 mượt mà cam kết cập nhật 5 năm liên tục qua chương trình webOS Re:New.",
                24, true,
                new Dictionary<string, string>
                {
                    ["Kích thước & Độ phân giải"] = "55 inch 4K Ultra HD (3840 x 2160) Pixel Dimming tự phát sáng",
                    ["Bộ xử lý hình ảnh"] = "Bộ xử lý AI α9 4K Thế hệ 7 (α9 AI Processor 4K Gen7)",
                    ["Tần số quét"] = "Native 120Hz (VRR Boost lên đến 144Hz cực mượt cho PS5 & PC Gaming)",
                    ["Định dạng HDR"] = "Dolby Vision, HDR10, HLG, Filmmaker Mode giữ nguyên ý đồ đạo diễn",
                    ["Âm thanh"] = "Hệ thống loa 2.2 kênh 40W, Giả lập âm thanh vòm AI Sound Pro 9.1.2 kênh",
                    ["Cổng kết nối"] = "4x HDMI 2.1 (4K @ 144Hz, eARC, ALLM), 3x USB, Wi-Fi 5, Bluetooth 5.1"
                },
                new List<(string, string, decimal, decimal, int, string)>
                {
                    ("LG-OLEDC4-55", "Smart TV LG OLED evo 55 inch C4 4K 144Hz Viền Siêu Mỏng", 36990000m, 32490000m, 12, "https://images.unsplash.com/photo-1593784991095-a205069470b6?auto=format&fit=crop&w=800&q=80")
                },
                new List<string>
                {
                    "https://images.unsplash.com/photo-1527443224154-c4a3942d3acf?auto=format&fit=crop&w=800&q=80"
                }
            )
        };

        foreach (var pData in productsToCreate)
        {
            var existing = await context.Products.FirstOrDefaultAsync(p => p.Slug == pData.Slug);
            if (existing == null)
            {
                var product = new Product
                {
                    Name = pData.Name,
                    Slug = pData.Slug,
                    CategoryId = pData.CatId,
                    BrandId = pData.BrandId,
                    SupplierId = pData.SupId,
                    FeaturedImage = pData.FeaturedImg,
                    ShortDescription = pData.ShortDesc,
                    FullDescription = pData.FullDesc,
                    WarrantyMonths = pData.Warranty,
                    IsFeatured = pData.IsFeatured,
                    IsActive = true,
                    ViewCount = new Random().Next(450, 4200),
                    CreatedAt = DateTime.UtcNow.AddDays(-new Random().Next(10, 120)),
                    SpecificationsJson = JsonSerializer.Serialize(pData.Specs)
                };

                context.Products.Add(product);
                await context.SaveChangesAsync();

                // Thêm Variants
                foreach (var v in pData.Variants)
                {
                    if (!await context.ProductVariants.AnyAsync(pv => pv.SKU == v.Sku))
                    {
                        context.ProductVariants.Add(new ProductVariant
                        {
                            ProductId = product.ProductId,
                            SKU = v.Sku,
                            VariantName = v.VarName,
                            OriginalPrice = v.OrigPrice,
                            SalePrice = v.SalePrice,
                            StockQuantity = v.Stock,
                            WeightGrams = 250,
                            ThumbnailImage = v.Img,
                            IsActive = true,
                            CreatedAt = product.CreatedAt
                        });
                    }
                }

                // Thêm Gallery Images
                int order = 1;
                foreach (var imgUrl in pData.GalleryImgs)
                {
                    context.ProductImages.Add(new ProductImage
                    {
                        ProductId = product.ProductId,
                        ImageUrl = imgUrl,
                        DisplayOrder = order++,
                        IsPrimary = (order == 2)
                    });
                }
                await context.SaveChangesAsync();
            }
        }
    }
    #endregion

    #region 6. Orders & OrderDetails & StatusHistory
    private static async Task SeedOrdersAsync(TechStoreDbContext context, List<User> customers, List<User> staff)
    {
        int currentOrderCount = await context.Orders.CountAsync();
        if (currentOrderCount >= 25) return;

        var allVariants = await context.ProductVariants.Include(v => v.Product).Where(v => v.IsActive).ToListAsync();
        if (!allVariants.Any()) return;

        var coupons = await context.Coupons.Where(c => c.IsActive).ToListAsync();

        var staffOrder = staff.FirstOrDefault(s => s.Username == "staff_order") ?? staff.FirstOrDefault();

        var orderTemplates = new (string Status, string PayMethod, string PayStatus, int DaysAgo, string Note)[]
        {
            ("Delivered", "VietQR", "Paid", 75, "Khách nhận hàng đầy đủ, đã kích hoạt bảo hành."),
            ("Delivered", "COD", "Paid", 68, "Giao giờ hành chính, đã thanh toán tiền mặt."),
            ("Delivered", "VietQR", "Paid", 55, "Giao hàng tại sảnh lễ tân tòa nhà."),
            ("Delivered", "COD", "Paid", 48, "Khách quen đặt thêm phụ kiện."),
            ("Delivered", "VietQR", "Paid", 40, "Đã gọi xác nhận và giao thành công."),
            ("Delivered", "VietQR", "Paid", 32, "Giao hỏa tốc 2 giờ nội thành."),
            ("Delivered", "COD", "Paid", 25, "Giao tận tay khách hàng."),
            ("Delivered", "VietQR", "Paid", 20, "Thanh toán chuyển khoản VietQR tự động."),
            ("Delivered", "COD", "Paid", 15, "Đã nhận hàng và ký biên bản nghiệm thu."),
            ("Delivered", "VietQR", "Paid", 12, "Giao thành công."),
            ("Shipping", "VietQR", "Paid", 3, "Đang vận chuyển liên tỉnh qua Viettel Post, mã vận đơn VT892104."),
            ("Shipping", "COD", "Pending", 2, "Tài xế Ahamove đang giao đến địa chỉ nhận hàng."),
            ("Shipping", "VietQR", "Paid", 1, "Đang giao chặng cuối, dự kiến đến trước 17h hôm nay."),
            ("Processing", "VietQR", "Paid", 2, "Kho tổng đang đóng gói kiểm tra số serial và seal."),
            ("Processing", "COD", "Pending", 1, "Đã in phiếu xuất kho, đang đóng thùng chống sốc."),
            ("Processing", "VietQR", "Paid", 1, "Chờ nhân viên đóng kiện hàng."),
            ("Confirmed", "VietQR", "Paid", 1, "Hệ thống VietQR nhận đủ tiền, đã phân bổ đơn cho bộ phận kho."),
            ("Confirmed", "COD", "Pending", 1, "Nhân viên CSKH đã gọi xác minh địa chỉ nhận hàng thành công."),
            ("Confirmed", "VietQR", "Paid", 0, "Đơn thanh toán chuyển khoản hợp lệ."),
            ("Pending", "VietQR", "Pending", 0, "Khách hàng vừa tạo đơn hàng, chờ chuyển khoản VietQR."),
            ("Pending", "COD", "Pending", 0, "Đơn hàng mới tạo, đang chờ tổng đài viên gọi xác nhận."),
            ("Pending", "VietQR", "Pending", 0, "Đang chờ thanh toán qua mã QR."),
            ("Cancelled", "COD", "Pending", 18, "Khách hàng đổi ý muốn đổi sang phiên bản dung lượng cao hơn."),
            ("Cancelled", "VietQR", "Pending", 10, "Quá thời gian thanh toán VietQR (15 phút), tự động hủy.")
        };

        var rand = new Random(42);
        int index = 1;

        foreach (var tpl in orderTemplates)
        {
            var customer = customers[rand.Next(customers.Count)];
            var address = await context.CustomerAddresses.FirstOrDefaultAsync(a => a.UserId == customer.UserId)
                          ?? new CustomerAddress
                          {
                              UserId = customer.UserId,
                              RecipientName = customer.FullName,
                              PhoneNumber = customer.PhoneNumber ?? "0909123456",
                              Province = "TP. Hồ Chí Minh",
                              District = "Quận 1",
                              Ward = "Bến Nghé",
                              StreetAddress = "123 Đường Lê Lợi"
                          };

            int itemCount = rand.Next(1, 3);
            var pickedVariants = allVariants.OrderBy(_ => rand.Next()).Take(itemCount).ToList();

            decimal subTotal = 0;
            var details = new List<OrderDetail>();

            foreach (var variant in pickedVariants)
            {
                int qty = rand.Next(1, 3);
                decimal unitPrice = variant.SalePrice > 0 ? variant.SalePrice : variant.OriginalPrice;
                subTotal += unitPrice * qty;

                details.Add(new OrderDetail
                {
                    VariantId = variant.VariantId,
                    ProductName = variant.Product?.Name ?? "Thiết bị công nghệ",
                    VariantName = variant.VariantName,
                    SKU = variant.SKU,
                    UnitPrice = unitPrice,
                    Quantity = qty
                });
            }

            decimal discount = 0;
            Coupon? usedCoupon = null;
            if (subTotal >= 3000000m && coupons.Any() && rand.Next(10) > 4)
            {
                usedCoupon = coupons[rand.Next(coupons.Count)];
                discount = usedCoupon.DiscountType == "Percentage"
                    ? Math.Min(subTotal * (usedCoupon.DiscountValue / 100m), usedCoupon.MaxDiscountAmount ?? 500000m)
                    : usedCoupon.DiscountValue;
            }

            decimal shippingFee = subTotal >= 1000000m ? 0m : 30000m;
            decimal totalAmount = Math.Max(0m, subTotal - discount + shippingFee);

            var createdDate = DateTime.UtcNow.AddDays(-tpl.DaysAgo).AddMinutes(rand.Next(10, 600));

            string orderCode = $"ORD{createdDate:yyyyMMdd}-{1000 + index}";

            if (await context.Orders.AnyAsync(o => o.OrderCode == orderCode))
            {
                orderCode = $"ORD{createdDate:yyyyMMdd}-{rand.Next(2000, 9999)}";
            }

            var order = new Order
            {
                OrderCode = orderCode,
                UserId = customer.UserId,
                CustomerName = address.RecipientName,
                CustomerPhone = address.PhoneNumber,
                CustomerEmail = customer.Email,
                ShippingAddress = $"{address.StreetAddress}, {address.Ward}, {address.District}, {address.Province}",
                OrderNotes = tpl.Note,
                CouponId = usedCoupon?.CouponId,
                DiscountAmount = discount,
                SubTotal = subTotal,
                ShippingFee = shippingFee,
                TotalAmount = totalAmount,
                PaymentMethod = tpl.PayMethod,
                PaymentStatus = tpl.PayStatus,
                OrderStatus = tpl.Status,
                CreatedAt = createdDate,
                UpdatedAt = createdDate.AddHours(rand.Next(1, 24))
            };

            context.Orders.Add(order);
            await context.SaveChangesAsync();

            foreach (var d in details)
            {
                d.OrderId = order.OrderId;
                context.OrderDetails.Add(d);
            }

            context.OrderStatusHistories.Add(new OrderStatusHistory
            {
                OrderId = order.OrderId,
                PreviousStatus = null,
                NewStatus = "Pending",
                Note = "Khách hàng đặt hàng thành công qua Website TechStore",
                ChangedBy = null,
                ChangedAt = createdDate
            });

            if (tpl.Status != "Pending")
            {
                context.OrderStatusHistories.Add(new OrderStatusHistory
                {
                    OrderId = order.OrderId,
                    PreviousStatus = "Pending",
                    NewStatus = tpl.Status == "Cancelled" ? "Cancelled" : "Confirmed",
                    Note = tpl.Status == "Cancelled" ? "Đơn hàng đã bị hủy theo yêu cầu" : "Hệ thống xác nhận đơn hàng thành công",
                    ChangedBy = staffOrder?.UserId,
                    ChangedAt = createdDate.AddMinutes(15)
                });
            }

            if (tpl.Status == "Processing" || tpl.Status == "Shipping" || tpl.Status == "Delivered")
            {
                context.OrderStatusHistories.Add(new OrderStatusHistory
                {
                    OrderId = order.OrderId,
                    PreviousStatus = "Confirmed",
                    NewStatus = "Processing",
                    Note = "Kho đã in phiếu xuất và hoàn tất đóng gói bảo vệ",
                    ChangedBy = staffOrder?.UserId,
                    ChangedAt = createdDate.AddHours(2)
                });
            }

            if (tpl.Status == "Shipping" || tpl.Status == "Delivered")
            {
                context.OrderStatusHistories.Add(new OrderStatusHistory
                {
                    OrderId = order.OrderId,
                    PreviousStatus = "Processing",
                    NewStatus = "Shipping",
                    Note = "Bàn giao cho đối tác vận chuyển giao hàng tận nơi",
                    ChangedBy = staffOrder?.UserId,
                    ChangedAt = createdDate.AddHours(4)
                });
            }

            if (tpl.Status == "Delivered")
            {
                context.OrderStatusHistories.Add(new OrderStatusHistory
                {
                    OrderId = order.OrderId,
                    PreviousStatus = "Shipping",
                    NewStatus = "Delivered",
                    Note = "Giao hàng thành công. Khách hàng đã nhận và kiểm tra nguyên seal.",
                    ChangedBy = staffOrder?.UserId,
                    ChangedAt = createdDate.AddHours(28)
                });
            }

            if (tpl.PayMethod == "VietQR" && tpl.PayStatus == "Paid")
            {
                context.PaymentTransactions.Add(new PaymentTransaction
                {
                    OrderId = order.OrderId,
                    PaymentMethod = "VietQR",
                    TransactionReference = $"MBB{createdDate:yyyyMMdd}{rand.Next(100000, 999999)}",
                    Amount = totalAmount,
                    Status = "Success",
                    BankCode = "MBBank",
                    ConfirmedBy = staffOrder?.UserId,
                    ConfirmedAt = createdDate.AddMinutes(5),
                    CreatedAt = createdDate
                });
            }

            await context.SaveChangesAsync();
            index++;
        }
    }
    #endregion

    #region 7. Product Reviews
    private static async Task SeedProductReviewsAsync(TechStoreDbContext context, List<User> customers)
    {
        int currentReviewCount = await context.ProductReviews.CountAsync();
        if (currentReviewCount >= 30) return;

        var products = await context.Products.Where(p => p.IsActive).ToListAsync();
        if (!products.Any() || !customers.Any()) return;

        var reviewTemplates = new (string Comment, int Rating)[]
        {
            ("Sản phẩm nguyên seal chính hãng, kích hoạt đủ 12 tháng bảo hành. Đóng gói 4 lớp bóng khí rất kỹ!", 5),
            ("Giao hàng siêu tốc trong 2 giờ nội thành. Máy cầm đầm tay, màn hình OLED màu sắc quá đẹp và nịnh mắt.", 5),
            ("Đã dùng thử 1 tuần, pin cực kỳ trâu bò, lướt web và làm việc cả ngày tối về vẫn còn 35%. Rất đáng tiền!", 5),
            ("Thiết kế quá sang trọng và đẳng cấp, chất lượng gia công không có điểm gì để chê. Nhân viên tư vấn nhiệt tình.", 5),
            ("Âm thanh trong trẻo, âm bass sâu chắc nịch không bị rè khi mở âm lượng lớn. Rất ưng ý!", 5),
            ("Hiệu năng mạnh mẽ ngoài mong đợi, render video 4K hay chơi game nặng đều mượt mà mát rượi.", 5),
            ("Hàng chính hãng VN/A check bảo hành chuẩn chỉ trên website của hãng. Đánh giá 5 sao cho shop!", 5),
            ("Màu sắc bên ngoài thực tế đẹp hơn trên hình chụp rất nhiều. Bàn phím gõ êm, touchpad nhạy mượt.", 5),
            ("Được tặng kèm voucher giảm giá cho lần mua sau. Shop phục vụ rất chuyên nghiệp và chu đáo.", 5),
            ("Máy chạy rất mượt, sạc nhanh chưa tới 40 phút là đầy pin. Trải nghiệm rất tuyệt vời.", 4),
            ("Sản phẩm tốt, đóng gói cẩn thận. Bên vận chuyển giao hơi trễ 1 chút nhưng tổng thể vẫn rất hài lòng.", 4),
            ("Giá cả cạnh tranh nhất thị trường so với các chuỗi lớn khác mà chính sách bảo hành tương đương.", 5)
        };

        var rand = new Random(101);

        foreach (var p in products)
        {
            int pReviewCount = await context.ProductReviews.CountAsync(r => r.ProductId == p.ProductId);
            if (pReviewCount >= 2) continue;

            int reviewsToAdd = rand.Next(1, 3);
            for (int i = 0; i < reviewsToAdd; i++)
            {
                var cust = customers[rand.Next(customers.Count)];
                var tpl = reviewTemplates[rand.Next(reviewTemplates.Length)];

                context.ProductReviews.Add(new ProductReview
                {
                    ProductId = p.ProductId,
                    UserId = cust.UserId,
                    Rating = tpl.Rating,
                    Comment = tpl.Comment,
                    IsApproved = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-rand.Next(2, 60))
                });
            }
        }
        await context.SaveChangesAsync();
    }
    #endregion

    #region 8. Inventory Transactions
    private static async Task SeedInventoryTransactionsAsync(TechStoreDbContext context, List<User> staff)
    {
        int currentTxCount = await context.InventoryTransactions.CountAsync();
        if (currentTxCount >= 25) return;

        var variants = await context.ProductVariants.Include(v => v.Product).Where(v => v.IsActive).ToListAsync();
        var suppliers = await context.Suppliers.Where(s => s.IsActive).ToListAsync();
        var staffKho = staff.FirstOrDefault(s => s.Username == "staff_kho") ?? staff.FirstOrDefault();

        if (staffKho == null || !variants.Any() || !suppliers.Any()) return;

        var rand = new Random(202);

        foreach (var variant in variants.Take(18))
        {
            var sup = suppliers.FirstOrDefault(s => s.SupplierId == variant.Product.SupplierId) ?? suppliers[rand.Next(suppliers.Count)];
            int importQty = rand.Next(50, 200);
            decimal costPrice = variant.OriginalPrice * 0.75m;

            context.InventoryTransactions.Add(new InventoryTransaction
            {
                VariantId = variant.VariantId,
                SupplierId = sup.SupplierId,
                TransactionType = "IMPORT",
                Quantity = importQty,
                UnitPrice = costPrice,
                ReferenceCode = $"PNK{DateTime.UtcNow:yyyyMM}-{rand.Next(1000, 9999)}",
                Note = $"Nhập lô hàng chính ngạch mới từ {sup.Name} theo hợp đồng cung ứng quý.",
                CreatedBy = staffKho.UserId,
                CreatedAt = DateTime.UtcNow.AddDays(-rand.Next(15, 90))
            });
        }

        await context.SaveChangesAsync();
    }
    #endregion

    #region 9. Coupons / Vouchers
    private static async Task SeedCouponsAsync(TechStoreDbContext context)
    {
        var vouchers = new (string Code, string Desc, string Type, decimal Val, decimal MinOrder, decimal? MaxDisc, int Limit, int Used)[]
        {
            ("TECHSTORE2026", "Giảm ngay 200.000đ cho đơn hàng thiết bị công nghệ từ 3.000.000đ", "FixedAmount", 200000m, 3000000m, null, 500, 78),
            ("VIPMEMBER10", "Giảm 10% tối đa 1.500.000đ cho thành viên VIP từ 10.000.000đ", "Percentage", 10m, 10000000m, 1500000m, 200, 42),
            ("FREESHIP50", "Miễn phí vận chuyển tận nhà giảm tối đa 50.000đ cho đơn từ 1.000.000đ", "FixedAmount", 50000m, 1000000m, null, 1000, 260),
            ("TANXUAN2026", "Lì xì công nghệ đầu năm giảm trực tiếp 500.000đ cho đơn từ 15.000.000đ", "FixedAmount", 500000m, 15000000m, null, 300, 56),
            ("GAMINGPRO", "Ưu đãi chuyên biệt 8% tối đa 2.000.000đ cho Laptop Gaming và Màn hình", "Percentage", 8m, 20000000m, 2000000m, 150, 31),
            ("AUDIOHEAVEN", "Giảm ngay 150.000đ cho toàn bộ dòng Tai nghe và Loa Bluetooth từ 2.000.000đ", "FixedAmount", 150000m, 2000000m, null, 400, 95)
        };

        foreach (var v in vouchers)
        {
            if (!await context.Coupons.AnyAsync(c => c.Code == v.Code))
            {
                context.Coupons.Add(new Coupon
                {
                    Code = v.Code,
                    Description = v.Desc,
                    DiscountType = v.Type,
                    DiscountValue = v.Val,
                    MinOrderAmount = v.MinOrder,
                    MaxDiscountAmount = v.MaxDisc,
                    UsageLimit = v.Limit,
                    UsageCount = v.Used,
                    StartDate = DateTime.UtcNow.AddDays(-30),
                    EndDate = DateTime.UtcNow.AddDays(335),
                    IsActive = true
                });
            }
        }
        await context.SaveChangesAsync();
    }
    #endregion

    #region 10. Blog Posts
    private static async Task SeedBlogPostsAsync(TechStoreDbContext context, List<User> staff)
    {
        int currentPostCount = await context.BlogPosts.CountAsync();
        if (currentPostCount >= 9) return;

        var author = staff.FirstOrDefault(s => s.Username == "staff_marketing") ?? staff.FirstOrDefault();
        if (author == null) return;

        var catReviews = await context.BlogCategories.FirstOrDefaultAsync(c => c.Slug == "danh-gia-tren-tay")
                         ?? await context.BlogCategories.FirstOrDefaultAsync();
        var catNews = await context.BlogCategories.FirstOrDefaultAsync(c => c.Slug == "tin-tuc-cong-nghe")
                      ?? catReviews;
        var catTips = await context.BlogCategories.FirstOrDefaultAsync(c => c.Slug == "meo-thu-thuat")
                      ?? catReviews;

        if (catReviews == null) return;

        var newPosts = new (string Title, string Slug, int CatId, string Summary, string Content, string Thumb)[]
        {
            (
                "Đánh giá chi tiết Samsung Galaxy Z Flip6: Màn hình phụ thông minh và AI đột phá",
                "danh-gia-chi-tiet-samsung-galaxy-z-flip6",
                catReviews.BlogCategoryId,
                "Khám phá sự nâng cấp vượt bậc trên chiếc smartphone màn hình gập vỏ sò đáng mua nhất năm 2026 với camera 50MP và Galaxy AI.",
                "<p>Samsung Galaxy Z Flip6 mang đến sự thay đổi toàn diện với thiết kế bản lề FlexHinge khít hoàn hảo, không còn khe hở và nếp gấp màn hình gần như biến mất khi vuốt ngón tay.</p><p>Hệ thống camera chính nâng cấp lên 50MP cho chất lượng ảnh chụp đêm vượt trội và tính năng Auto Zoom tự động căn chỉnh khung hình khi đặt máy gập trên bàn.</p>",
                "https://images.unsplash.com/photo-1592899677977-9c10ca588bbd?auto=format&fit=crop&w=800&q=80"
            ),
            (
                "Top 5 Màn hình Gaming OLED chuẩn eSports 240Hz đáng mua nhất năm 2026",
                "top-5-man-hinh-gaming-oled-tot-nhat-2026",
                catNews?.BlogCategoryId ?? catReviews.BlogCategoryId,
                "Tổng hợp các siêu phẩm màn hình chơi game QD-OLED và WOLED với độ trễ siêu tốc 0.03ms và màu sắc chuẩn HDR rạp phim.",
                "<p>Năm 2026 đánh dấu sự bùng nổ của công nghệ màn hình OLED trên các dòng màn hình máy tính chơi game. Nhờ thời gian đáp ứng tức thì 0.03ms và màu đen sâu vô tận, hiện tượng bóng ma nhòe hình (ghosting) đã hoàn toàn bị loại bỏ.</p>",
                "https://images.unsplash.com/photo-1527443224154-c4a3942d3acf?auto=format&fit=crop&w=800&q=80"
            ),
            (
                "Hướng dẫn chọn mua Laptop Doanh nhân mỏng nhẹ bền bỉ: ThinkPad X1 vs Zenbook 14",
                "so-sanh-thinkpad-x1-vs-zenbook-14-oled",
                catTips?.BlogCategoryId ?? catReviews.BlogCategoryId,
                "Phân tích chuyên sâu giữa độ bền bỉ chuẩn quân đội của ThinkPad và phong cách thời trang rực rỡ của ASUS Zenbook Lumina OLED.",
                "<p>Nếu bạn thường xuyên di chuyển công tác, cần một chiếc laptop gõ phím êm ái và bảo mật tuyệt đối thì ThinkPad X1 Carbon Gen 12 là sự lựa chọn không đối thủ. Ngược lại, nếu bạn làm công việc sáng tạo nội dung cần màn hình 3K OLED 120Hz rực rỡ thì Zenbook 14 UX3405 là ứng cử viên số 1.</p>",
                "https://images.unsplash.com/photo-1496181133206-80ce9b88a853?auto=format&fit=crop&w=800&q=80"
            ),
            (
                "Trải nghiệm âm thanh Marshall Emberton II: Chiếc loa di động kháng nước chuẩn IP67",
                "trai-nghiem-loa-bluetooth-marshall-emberton-ii",
                catReviews.BlogCategoryId,
                "Chất âm rock mộc mạc, thời lượng pin ấn tượng hơn 30 giờ cùng khả năng chống bụi nước hoàn hảo trong thân hình nhỏ gọn.",
                "<p>Marshall Emberton II tiếp tục kế thừa phong cách hoài cổ đậm chất Anh Quốc với chất âm vocal ấm áp, dải bass vừa vặn và khả năng chống chịu thời tiết khắc nghiệt chuẩn IP67 lý tưởng cho mọi chuyến dã ngoại.</p>",
                "https://images.unsplash.com/photo-1545454675-3531b543be5d?auto=format&fit=crop&w=800&q=80"
            )
        };

        foreach (var p in newPosts)
        {
            if (!await context.BlogPosts.AnyAsync(b => b.Slug == p.Slug))
            {
                context.BlogPosts.Add(new BlogPost
                {
                    Title = p.Title,
                    Slug = p.Slug,
                    BlogCategoryId = p.CatId,
                    AuthorId = author.UserId,
                    Summary = p.Summary,
                    Content = p.Content,
                    ThumbnailUrl = p.Thumb,
                    ViewCount = new Random().Next(320, 2500),
                    IsPublished = true,
                    PublishedAt = DateTime.UtcNow.AddDays(-new Random().Next(3, 45))
                });
            }
        }
        await context.SaveChangesAsync();
    }
    #endregion
}


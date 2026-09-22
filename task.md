# Danh Sách Công Việc Phase 1: Hoàn Thiện Admin Panel (Hoàn thành 100%)

- [x] **Bước 0: Hạ tầng & Chuẩn bị**
  - [x] Thêm các Permission còn thiếu (`Dashboard.View`, `Categories.Manage`, `Brands.Manage`) vào DB
  - [x] Xây dựng tiện ích Upload File local (`IFileStorageService` & `LocalFileStorageService`) lưu vào `wwwroot/uploads/`
  - [x] Cập nhật Sidebar Navigation trong `_AdminLayout.cshtml` đầy đủ các mục (Tổng quan, Sản phẩm, Danh mục, Thương hiệu, Khuyến mãi, Đơn hàng, Banner, Phân quyền, Nhật ký)
- [x] **Bước 1: 1.1 Admin Dashboard (Tổng quan hệ thống)**
  - [x] Tạo `DashboardController.cs` với `[HasPermission("Dashboard.View")]`
  - [x] Thống kê: Doanh thu theo tháng, Tổng đơn hàng, Chờ duyệt VietQR, Cảnh báo tồn kho thấp (<= 20 cái), Đơn hàng mới nhất, Nhật ký gần nhất
  - [x] Tạo View `Dashboard/Index.cshtml` giao diện Dashboard đẹp mắt, biểu đồ Chart.js, card chỉ số ấn tượng
- [x] **Bước 2: 1.2 Quản lý Sản phẩm (Product CRUD & Biến thể)**
  - [x] Tạo `ProductController.cs` với đầy đủ quyền `[HasPermission("Products.View")]`, `Create`, `Edit`, `Delete`
  - [x] Chức năng lọc sản phẩm: theo danh mục, thương hiệu, trạng thái, tìm kiếm từ khóa, phân trang
  - [x] Chức năng Toggle Active, Toggle Featured (AJAX)
  - [x] Tạo View `Product/Index.cshtml` (danh sách hiện đại, huy hiệu tồn kho, giá bán, thao tác nhanh)
  - [x] Tạo View `Product/Create.cshtml` và `Product/Edit.cshtml` với form chi tiết, upload ảnh đại diện, tự sinh Slug
  - [x] Quản lý Biến thể (Variants: Thêm, sửa, xóa biến thể với SKU, giá bán, tồn kho)
- [x] **Bước 3: 1.3 Quản lý Danh mục (Category CRUD)**
  - [x] Tạo `CategoryController.cs` với `[HasPermission("Categories.Manage")]`
  - [x] Quản lý phân cấp cha - con (Parent - Child), thứ tự hiển thị (DisplayOrder), trạng thái
  - [x] Tạo View `Category/Index.cshtml` với Modal/Form thêm mới & chỉnh sửa trực quan
- [x] **Bước 4: 1.4 Quản lý Thương hiệu (Brand CRUD)**
  - [x] Tạo `BrandController.cs` với `[HasPermission("Brands.Manage")]`
  - [x] Quản lý tên, slug, mô tả, upload ảnh Logo thương hiệu
  - [x] Tạo View `Brand/Index.cshtml` với Modal/Form thêm mới & chỉnh sửa
- [x] **Bước 5: 1.5 Quản lý Khuyến mãi (Coupon CRUD)**
  - [x] Tạo `CouponController.cs` với `[HasPermission("Coupons.Manage")]`
  - [x] Quản lý mã, loại giảm giá (Cố định / Phần trăm), giá trị giảm, đơn tối thiểu, số lượng sử dụng, hạn dùng
  - [x] Tạo View `Coupon/Index.cshtml` với Modal/Form thêm mới & chỉnh sửa
- [x] **Bước 6: Kiểm tra & Xác minh Phase 1**
  - [x] `dotnet build` đạt 0 errors, 0 warnings
  - [x] Khởi chạy Web Server & Test kiểm thử xác thực HTTP 200 cho toàn bộ 10 route Admin

---

# Danh Sách Công Việc Phase 2: Hoàn Thiện Storefront (Trải nghiệm khách hàng) - (Hoàn thành 100%)

- [x] **2.1. Đăng ký tài khoản (Register) & Tích hợp Header**
  - [x] Thêm Action `Register` (GET/POST) vào `AuthController.cs` với validation (Username, Email, FullName, PhoneNumber, Password, ConfirmPassword)
  - [x] Gán vai trò `Customer` (RoleId = 5), băm mật khẩu Identity PBKDF2 và tự động đăng nhập Cookie Auth sau khi đăng ký
  - [x] Tạo View `Views/Auth/Register.cshtml` chuẩn phong cách Best Buy hiện đại
  - [x] Cập nhật Header `_Layout.cshtml`: Hiển thị trạng thái đăng nhập/đăng xuất, tên người dùng và dropdown menu tài khoản
- [x] **2.2. Hồ sơ cá nhân (Profile) & Lịch sử đơn hàng**
  - [x] Tạo `AccountController.cs` với các Action `Profile` (xem/sửa họ tên, số điện thoại, email), `ChangePassword`, `Orders` (danh sách đơn đã đặt), `OrderDetail` (chi tiết đơn hàng)
  - [x] Tạo View `Views/Account/Profile.cshtml` (Quản lý thông tin cá nhân & Đổi mật khẩu)
  - [x] Tạo View `Views/Account/Orders.cshtml` (Danh sách đơn hàng của tôi, trạng thái xử lý, tổng tiền)
  - [x] Tạo View `Views/Account/OrderDetail.cshtml` (Chi tiết các mặt hàng, trạng thái thanh toán VietQR / COD)
- [x] **2.3. Trang Giới thiệu & Liên hệ**
  - [x] Tạo View `Views/Home/About.cshtml` (Giới thiệu TechStore, cam kết chất lượng, mạng lưới đối tác Apple, Samsung, Sony...)
  - [x] Tạo Action & View `Views/Home/Contact.cshtml` (Form liên hệ, bản đồ, hotline, gửi phản hồi)
- [x] **2.4. Tìm kiếm Autocomplete thời gian thực**
  - [x] Tạo API endpoint `/api/product/suggest?q=...` trả về gợi ý top 5-6 sản phẩm khớp từ khóa (ảnh, tên, giá bán, slug, thương hiệu, danh mục)
  - [x] Tích hợp JS gợi ý tìm kiếm tức thì vào Search Bar trên Header `_Layout.cshtml` (`search_suggest.js` với bàn phím up/down/enter, debounce, highlight keyword)
- [x] **2.5. Kiểm thử & Xác minh Phase 2**
  - [x] `dotnet build` đạt 0 errors, 0 warnings
  - [x] Kiểm thử luồng Đăng ký -> Tự động đăng nhập -> Xem Profile -> Đặt hàng -> Xem chi tiết đơn hàng & VietQR -> Đổi mật khẩu/Cập nhật profile -> Tìm kiếm gợi ý -> About & Contact (Toàn bộ HTTP 200/302 OK)

---

# Danh Sách Công Việc Phase 3: Đánh giá sản phẩm & Nâng cao - (Hoàn thành 100%)

- [x] **3.1. Hệ thống Đánh giá & Bình luận (Product Reviews & Ratings)**
  - [x] Tạo Entity `ProductReview.cs`, cấu hình `TechStoreDbContext.cs`, thêm permission `Reviews.Manage`
  - [x] Trang chi tiết sản phẩm: Huy hiệu sao trung bình, Score Card, Star breakdown bar, form chấm sao 1-5 sao và viết nhận xét
  - [x] Quản trị kiểm duyệt: `ReviewController.cs` & `Review/Index.cshtml` trong Admin Panel với bộ lọc Tất cả, Đã duyệt, Đã ẩn, thao tác Duyệt/Ẩn/Xóa
- [x] **3.2. Danh sách yêu thích (Wishlist)**
  - [x] Tạo Entity `Wishlist.cs`, cấu hình Composite Key trong `TechStoreDbContext.cs`
  - [x] `WishlistApiController.cs`: API toggle thả tim thời gian thực và lấy số lượng badge
  - [x] `AccountController.cs` & `Views/Account/Wishlist.cshtml`: Trang quản lý bộ sưu tập yêu thích của khách hàng
  - [x] Tích hợp nút Trái Tim và Badge đếm trên Header `_Layout.cshtml`, nút Thả tim trên trang chi tiết sản phẩm
- [x] **3.3. Tự động hóa thông báo & Email xác nhận đơn hàng**
  - [x] `IEmailService.cs` & `EmailService.cs`: Tạo template hóa đơn HTML chuẩn Best Buy với danh sách mặt hàng, giá bán, mã VietQR và hướng dẫn chuyển khoản MBBank
  - [x] Tự động lưu bản sao hóa đơn email vào `wwwroot/uploads/emails/{OrderCode}.html` để kiểm tra offline và hỗ trợ gửi SMTP
  - [x] Tích hợp gửi email tự động ngay khi khách đặt hàng thành công tại `CheckoutController.PlaceOrder`
- [x] **3.4. Quản lý kho hàng nâng cao & Báo cáo xuất nhập tồn**
  - [x] Tạo Entity `InventoryTransaction.cs`, cấu hình bảng `InventoryTransactions`
  - [x] `InventoryController.cs` & các View `Index.cshtml`, `Import.cshtml`, `Transactions.cshtml`: Theo dõi tồn kho thực tế, cảnh báo mức an toàn (&le;10), lập phiếu nhập hàng mới và sổ nhật ký giao dịch kho
  - [x] Tự động ghi nhận giao dịch xuất kho `EXPORT_ORDER` khi khách đặt mua hàng qua Checkout
  - [x] Cập nhật thanh Menu Admin: Thêm mục "Kho & Nhập Hàng" và "Đánh Giá SP"
- [x] **3.5. Kiểm thử & Xác minh Phase 3**
  - [x] `dotnet build` đạt 0 errors, 0 warnings
  - [x] Kiểm thử toàn bộ 5 luồng: Admin Login &rarr; Đánh giá sản phẩm &rarr; Thả tim Wishlist &rarr; Đặt hàng sinh email VietQR &rarr; Nhập kho & Kiểm tra sổ giao dịch (Toàn bộ HTTP 200/302 OK)

---

# Danh Sách Công Việc Phase 4: Tin Tức Công Nghệ, Sổ Địa Chỉ Khách Hàng, Nhà Cung Cấp & Lịch Sử Đơn Hàng - (Hoàn thành 100%)

- [x] **4.1. Phân Hệ CMS Tin Tức & Đánh Giá Công Nghệ (CMS Tech News & Blogs)**
  - [x] Tạo Entity `BlogPost.cs` và `BlogCategory.cs`, cấu hình `TechStoreDbContext.cs`, thêm permission `Blogs.Manage`
  - [x] Storefront: `BlogController.cs` & `Views/Blog/Index.cshtml` (Tạp chí công nghệ Best Buy, bộ lọc danh mục, phân trang)
  - [x] Storefront: `Views/Blog/Detail.cshtml` (Nội dung chi tiết, ảnh bìa, lượt xem, bài viết liên quan)
  - [x] Storefront Header: Thêm liên kết "Tin Công Nghệ" trên thanh danh mục toàn hệ thống
  - [x] Admin CMS: `Areas/Admin/Controllers/BlogController.cs` & các View `Index.cshtml`, `Create.cshtml`, `Edit.cshtml`, `Categories.cshtml` (Soạn thảo bài viết, upload thumbnail, tự sinh slug, quản lý chuyên mục)
- [x] **4.2. Sổ Địa Chỉ Khách Hàng & Tự Động Điền Thanh Toán (Customer Address Book & Checkout Autofill)**
  - [x] Tạo Entity `CustomerAddress.cs`, cấu hình `TechStoreDbContext.cs`
  - [x] Storefront: `AccountController.Addresses`, `CreateAddress`, `SetDefaultAddress`, `DeleteAddress` & `Views/Account/Addresses.cshtml`
  - [x] Storefront Header: Tích hợp liên kết "Sổ địa chỉ nhận hàng" trong menu dropdown tài khoản
  - [x] Checkout: `CheckoutController.Index` & `Views/Checkout/Index.cshtml` tự động điền trước thông tin người nhận, SĐT, địa chỉ giao hàng từ địa chỉ mặc định
- [x] **4.3. Quản Lý Nhà Cung Cấp & Tích Hợp Nhập Kho (Suppliers Management & Inward Inventory)**
  - [x] Tạo Entity `Supplier.cs`, cấu hình `TechStoreDbContext.cs`, thêm permission `Suppliers.Manage`
  - [x] Admin CMS: `Areas/Admin/Controllers/SupplierController.cs` & `Supplier/Index.cshtml` (Danh bạ nhà cung cấp, thêm/sửa qua modal, bật/tắt hợp tác)
  - [x] Admin Sidebar: Thêm mục "Nhà Cung Cấp" dưới nhóm "Cửa Hàng"
  - [x] Tích hợp Nhập Kho: Dropdown chọn nhà cung cấp trong `InventoryController.Import` & `Views/Inventory/Import.cshtml`, ghi nhận NCC vào nhật ký giao dịch kho
- [x] **4.4. Truy Vết Tiến Trình Trạng Thái Đơn Hàng (Order Status History & Audit Timeline)**
  - [x] Tạo Entity `OrderStatusHistory.cs`, cấu hình `TechStoreDbContext.cs`
  - [x] Tự động ghi nhận lịch sử trạng thái ban đầu khi khách đặt hàng (`Pending`)
  - [x] Tự động ghi nhận lịch sử trạng thái khi duyệt thanh toán VietQR (`Processing`)
  - [x] Tự động ghi nhận lịch sử trạng thái khi nhân viên cập nhật tiến trình (`Shipping`, `Delivered`, `Cancelled`)
  - [x] Admin Order Detail: `Areas/Admin/Views/Order/Detail.cshtml` hiển thị Dòng thời gian kiểm toán (Audit Timeline) và form cập nhật tiến trình
  - [x] Khách hàng Order Detail: `Views/Account/OrderDetail.cshtml` hiển thị khối Nhật Ký Đơn Hàng theo thời gian thực
- [x] **4.5. Kiểm Thử & Xác Minh Phase 4**
  - [x] `dotnet build` đạt 0 errors, 0 warnings
  - [x] Kịch bản kiểm thử tự động `test_phase4.ps1` kiểm tra toàn bộ 5 module: Blog Storefront, Address Book & Checkout Autofill, Suppliers & Inventory Import, Admin Blog CMS, Order Status History & Audit Timeline (Toàn bộ PASS 100%)

---

# Danh Sách Công Việc Phase 5: Quản Lý Khách Hàng 360°, Báo Cáo Doanh Thu, Thư Viện Ảnh Sản Phẩm & Hệ Thống Thông Báo Toast - (Hoàn thành 100%)

- [x] **5.1. Phân Hệ Quản Lý Người Dùng & Hồ Sơ Khách Hàng 360° (Customer & User Management)**
  - [x] Tạo `UserViewModels.cs` (`UserListViewModel`, `UserDetailViewModel`) trong `Areas/Admin/Models/`
  - [x] Tạo `UserController.cs` trong `Areas/Admin/Controllers/` với `[HasPermission("Users.View")]` và `[HasPermission("Users.Edit")]`
  - [x] Chức năng danh sách người dùng (`Index`): Thống kê tổng tài khoản, khách hàng, ban quản trị, tài khoản khóa; tìm kiếm theo Tên/Username/Email/SĐT; lọc theo vai trò; phân trang
  - [x] Chức năng Hồ Sơ Khách Hàng 360° (`Detail`): Thống kê trọn đời (Tổng chi tiêu LTV, Tổng đơn đã đặt, Đơn hoàn tất, Đơn hủy); Thông tin tài khoản; Sổ địa chỉ giao hàng; Lịch sử đơn hàng; Đánh giá đã gửi; Sản phẩm yêu thích
  - [x] Chức năng Khóa/Mở khóa tài khoản (`ToggleLock`) kèm ghi log kiểm toán (AuditLog)
  - [x] Chức năng Đặt lại mật khẩu (`ResetPassword`) bởi Admin sử dụng `PasswordHasher<User>` kèm ghi log kiểm toán
  - [x] Chức năng Cập nhật phân quyền vai trò (`UpdateRoles`) kèm cơ chế bảo vệ tài khoản SuperAdmin
  - [x] Tạo View `Areas/Admin/Views/User/Index.cshtml` và `Detail.cshtml` phong cách hiện đại
- [x] **5.2. Báo Cáo Hoạt Động Kinh Doanh, Doanh Thu & Xuất Dữ Liệu CSV (Analytics & Reporting)**
  - [x] Seed Permission `Analytics.View` vào bảng `Permissions` và gán cho `SUPERADMIN`
  - [x] Tạo `AnalyticsViewModels.cs` (`AnalyticsDashboardViewModel`, `MonthlyRevenueItem`, `TopProductItem`)
  - [x] Tạo `AnalyticsController.cs` trong `Areas/Admin/Controllers/` với `[HasPermission("Analytics.View")]`
  - [x] Dashboard Phân tích (`Index`): 5 thẻ KPI tài chính (Tổng doanh thu, Doanh thu tháng, Tổng số đơn, Giá trị đơn TB AOV, Khách mới); Biểu đồ cột xu hướng doanh thu 6 tháng; Cơ cấu cổng thanh toán VietQR vs COD; Phân bổ trạng thái đơn hàng; Top 5 sản phẩm bán chạy nhất; Đơn hàng giá trị cao gần nhất
  - [x] Xuất báo cáo Đơn hàng (`ExportOrdersCsv`): UTF-8 BOM, đầy đủ thông tin khách, địa chỉ, hình thức TT, doanh thu
  - [x] Xuất báo cáo Khách hàng (`ExportCustomersCsv`): UTF-8 BOM, mã KH, họ tên, liên hệ, ngày tham gia, tổng đơn, tổng chi tiêu
  - [x] Xuất báo cáo Tồn kho (`ExportInventoryCsv`): UTF-8 BOM, mã SP, tên SKU, giá vốn, giá bán, số lượng tồn, cảnh báo kho
  - [x] Tạo View `Areas/Admin/Views/Analytics/Index.cshtml` trực quan, thẩm mỹ cao
- [x] **5.3. Thư Viện Ảnh Sản Phẩm Đa Góc Chụp (Multi-angle Product Gallery Images)**
  - [x] Cập nhật Entity `Product.cs` thêm navigation collection `Images` (`ICollection<ProductImage>`)
  - [x] Cập nhật `TechStoreDbContext.cs`: Khai báo `DbSet<ProductImage> ProductImages` và cấu hình Fluent API quan hệ 1-N với `Product`
  - [x] Admin: Cập nhật `Areas/Admin/Controllers/ProductController.cs` thêm `UploadGallery` (hỗ trợ tải lên cùng lúc nhiều ảnh góc chụp) và `DeleteGalleryImage`
  - [x] Admin View: `Areas/Admin/Views/Product/Edit.cshtml` tích hợp thẻ "Thư Viện Ảnh Bổ Sung" với upload nhiều ảnh, lưới xem trước và nút xóa nhanh
  - [x] Storefront View: `Views/Product/Detail.cshtml` tích hợp thanh chuyển đổi thumbnail ảnh đa góc chụp tương tác mượt mà (`switchGalleryImage`)
- [x] **5.4. Hệ Thống Thông Báo Toast Tương Tác & Cập Nhật Sidebar Admin (Toast Notifications & Navigation)**
  - [x] Xây dựng module `wwwroot/js/toast.js` hỗ trợ gọi `window.showToast(message, type, duration)` với các loại success, error, warning, info kèm animation trượt và tự động hủy
  - [x] Tích hợp `toast.js` vào `Views/Shared/_Layout.cshtml` và tự động kích hoạt khi có `TempData["SuccessMessage"]` / `TempData["ErrorMessage"]`
  - [x] Tích hợp `toast.js` vào `Areas/Admin/Views/Shared/_AdminLayout.cshtml`
  - [x] Bổ sung menu "Báo Cáo Doanh Thu" (`/Admin/Analytics`) và "Người Dùng & KH" (`/Admin/User`) vào Sidebar Admin Layout
- [x] **5.5. Kiểm Thử & Xác Minh Toàn Diện Phase 5**
  - [x] `dotnet build` đạt 0 errors, 0 warnings
  - [x] Kịch bản kiểm thử tự động `test_phase5.ps1` kiểm tra toàn bộ 4 phân hệ: Admin User Management & Customer 360, Analytics & CSV Export (Orders, Customers, Inventory), Multi-angle Product Gallery & Storefront Switcher, Toast Notifications & Admin Navigation (Toàn bộ PASS 100%)



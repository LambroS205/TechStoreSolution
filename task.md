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

# Danh Sách Công Việc Phase 3: Đánh giá sản phẩm & Nâng cao (Tiếp theo)

- [ ] **3.1. Hệ thống Đánh giá & Bình luận (Product Reviews & Ratings)**
  - [ ] Khách hàng đã mua sản phẩm có thể để lại đánh giá sao (1-5 sao) và nhận xét
  - [ ] Kiểm duyệt đánh giá trong Admin Panel
- [ ] **3.2. Danh sách yêu thích (Wishlist)**
  - [ ] Thêm sản phẩm yêu thích (lưu LocalStorage hoặc đồng bộ Account)
- [ ] **3.3. Tự động hóa thông báo & Email xác nhận đơn hàng**
  - [ ] Gửi email xác nhận đặt hàng và mã VietQR qua SMTP / background job
- [ ] **3.4. Quản lý kho hàng nâng cao & Báo cáo xuất nhập tồn**

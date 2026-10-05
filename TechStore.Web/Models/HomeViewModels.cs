using System.ComponentModel.DataAnnotations;
using TechStore.Core.Entities;

namespace TechStore.Web.Models;

/// <summary>
/// ViewModel dành cho biểu mẫu liên hệ khách hàng
/// </summary>
public class ContactFormViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập họ và tên của bạn")]
    [Display(Name = "Họ và tên")]
    [StringLength(100, ErrorMessage = "Họ và tên không vượt quá 100 ký tự")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập địa chỉ email")]
    [EmailAddress(ErrorMessage = "Địa chỉ email không hợp lệ")]
    [Display(Name = "Email")]
    [StringLength(150, ErrorMessage = "Email không vượt quá 150 ký tự")]
    public string Email { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Số điện thoại không đúng định dạng")]
    [Display(Name = "Số điện thoại")]
    [StringLength(20, ErrorMessage = "Số điện thoại không vượt quá 20 ký tự")]
    public string? PhoneNumber { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tiêu đề liên hệ")]
    [Display(Name = "Tiêu đề")]
    [StringLength(200, ErrorMessage = "Tiêu đề không vượt quá 200 ký tự")]
    public string Subject { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập nội dung tin nhắn")]
    [Display(Name = "Nội dung")]
    [StringLength(2000, ErrorMessage = "Nội dung không vượt quá 2000 ký tự")]
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// ViewModel tổng hợp dữ liệu trang chủ (Sliders, Banners, Danh mục, Sản phẩm nổi bật, Thương hiệu)
/// </summary>
public class HomeViewModel
{
    public List<Banner> HeroSliders { get; set; } = new();
    public List<Banner> SubBanners { get; set; } = new();
    public List<Category> Categories { get; set; } = new();
    public List<Product> FeaturedProducts { get; set; } = new();
    public List<Brand> Brands { get; set; } = new();
}

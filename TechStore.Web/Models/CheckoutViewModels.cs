using System.ComponentModel.DataAnnotations;

namespace TechStore.Web.Models;

/// <summary>
/// ViewModel phục vụ quá trình đặt hàng và thanh toán tại quầy Storefront
/// </summary>
public class PlaceOrderViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập họ tên người nhận hàng")]
    [Display(Name = "Họ và tên")]
    [StringLength(100, ErrorMessage = "Họ và tên không quá 100 ký tự")]
    public string CustomerName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập số điện thoại người nhận hàng")]
    [Phone(ErrorMessage = "Số điện thoại không đúng định dạng")]
    [Display(Name = "Số điện thoại")]
    [StringLength(20, ErrorMessage = "Số điện thoại không quá 20 ký tự")]
    public string CustomerPhone { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Email không đúng định dạng")]
    [Display(Name = "Email")]
    [StringLength(150, ErrorMessage = "Email không quá 150 ký tự")]
    public string? CustomerEmail { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn Tỉnh/Thành phố")]
    [Display(Name = "Tỉnh / Thành phố")]
    public string Province { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng chọn Quận/Huyện")]
    [Display(Name = "Quận / Huyện")]
    public string District { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng chọn Phường/Xã")]
    [Display(Name = "Phường / Xã")]
    public string Ward { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập số nhà, tên đường chi tiết")]
    [Display(Name = "Địa chỉ chi tiết")]
    [StringLength(255, ErrorMessage = "Địa chỉ chi tiết không quá 255 ký tự")]
    public string AddressDetail { get; set; } = string.Empty;

    [Display(Name = "Ghi chú đơn hàng")]
    [StringLength(500, ErrorMessage = "Ghi chú không quá 500 ký tự")]
    public string? OrderNotes { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn phương thức thanh toán")]
    [Display(Name = "Phương thức thanh toán")]
    public string PaymentMethod { get; set; } = "COD"; // "COD" hoặc "VietQR"

    [Display(Name = "Mã giảm giá")]
    [StringLength(50)]
    public string? CouponCode { get; set; }

    public string CartItemsJson { get; set; } = "[]";
}

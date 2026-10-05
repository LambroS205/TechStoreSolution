using System.ComponentModel.DataAnnotations;

namespace TechStore.Web.Models;

/// <summary>
/// ViewModel đăng nhập tài khoản
/// </summary>
public class LoginViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập hoặc email")]
    [Display(Name = "Tên đăng nhập hoặc Email")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
    [DataType(DataType.Password)]
    [Display(Name = "Mật khẩu")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Ghi nhớ đăng nhập")]
    public bool RememberMe { get; set; } = false;
}

/// <summary>
/// ViewModel đăng ký tài khoản khách hàng mới
/// </summary>
public class RegisterViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập")]
    [Display(Name = "Tên đăng nhập")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Tên đăng nhập phải từ 3 đến 50 ký tự")]
    [RegularExpression(@"^[a-zA-Z0-9_\.]+$", ErrorMessage = "Tên đăng nhập chỉ bao gồm chữ cái, số, dấu gạch dưới hoặc dấu chấm")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập họ và tên")]
    [Display(Name = "Họ và tên")]
    [StringLength(100, ErrorMessage = "Họ và tên không quá 100 ký tự")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập địa chỉ email")]
    [EmailAddress(ErrorMessage = "Địa chỉ email không đúng định dạng")]
    [Display(Name = "Email")]
    [StringLength(150, ErrorMessage = "Email không quá 150 ký tự")]
    public string Email { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Số điện thoại không đúng định dạng")]
    [Display(Name = "Số điện thoại")]
    [StringLength(20, ErrorMessage = "Số điện thoại không quá 20 ký tự")]
    public string? PhoneNumber { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
    [DataType(DataType.Password)]
    [Display(Name = "Mật khẩu")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Mật khẩu phải từ 8 ký tự trở lên")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu")]
    [DataType(DataType.Password)]
    [Display(Name = "Xác nhận mật khẩu")]
    [Compare(nameof(Password), ErrorMessage = "Mật khẩu xác nhận không khớp với mật khẩu đã nhập")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

/// <summary>
/// ViewModel yêu cầu đặt lại mật khẩu qua email
/// </summary>
public class ForgotPasswordViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập địa chỉ email của tài khoản")]
    [EmailAddress(ErrorMessage = "Địa chỉ email không đúng định dạng")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;
}

/// <summary>
/// ViewModel hoàn tất đặt lại mật khẩu mới từ liên kết mã xác thực
/// </summary>
public class ResetPasswordViewModel
{
    [Required(ErrorMessage = "Mã xác thực không hợp lệ hoặc thiếu")]
    public string Token { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới")]
    [DataType(DataType.Password)]
    [Display(Name = "Mật khẩu mới")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Mật khẩu phải từ 8 ký tự trở lên")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập xác nhận mật khẩu")]
    [DataType(DataType.Password)]
    [Display(Name = "Xác nhận mật khẩu mới")]
    [Compare(nameof(NewPassword), ErrorMessage = "Mật khẩu xác nhận không khớp")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

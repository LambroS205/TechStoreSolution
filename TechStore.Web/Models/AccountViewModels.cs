using System;
using System.ComponentModel.DataAnnotations;

namespace TechStore.Web.Models;

/// <summary>
/// ViewModel hiển thị hồ sơ thông tin cá nhân khách hàng và thống kê đơn hàng
/// </summary>
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

/// <summary>
/// DTO/ViewModel cập nhật thông tin cá nhân người dùng
/// </summary>
public class ProfileUpdateDto
{
    [Required(ErrorMessage = "Vui lòng nhập họ và tên")]
    [Display(Name = "Họ và tên")]
    [StringLength(100, ErrorMessage = "Họ và tên không quá 100 ký tự")]
    public string FullName { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Số điện thoại không đúng định dạng")]
    [Display(Name = "Số điện thoại")]
    [StringLength(20, ErrorMessage = "Số điện thoại không quá 20 ký tự")]
    public string? PhoneNumber { get; set; }
}

/// <summary>
/// DTO/ViewModel đổi mật khẩu tài khoản
/// </summary>
public class ChangePasswordDto
{
    [Required(ErrorMessage = "Vui lòng nhập mật khẩu hiện tại")]
    [DataType(DataType.Password)]
    [Display(Name = "Mật khẩu hiện tại")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới")]
    [DataType(DataType.Password)]
    [Display(Name = "Mật khẩu mới")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Mật khẩu mới phải từ 8 ký tự trở lên")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu mới")]
    [DataType(DataType.Password)]
    [Display(Name = "Xác nhận mật khẩu mới")]
    [Compare(nameof(NewPassword), ErrorMessage = "Mật khẩu xác nhận không khớp với mật khẩu mới")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

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

    // VIP TechMember Tiers: Member (<10M), Silver (10M-30M), Gold (30M-70M), Diamond (>=70M)
    public string VipTier => TotalSpent switch
    {
        >= 70_000_000m => "Diamond",
        >= 30_000_000m => "Gold",
        >= 10_000_000m => "Silver",
        _ => "Member"
    };

    public string VipTierDisplayName => VipTier switch
    {
        "Diamond" => "TechMember Diamond 💎",
        "Gold" => "TechMember Gold 🥇",
        "Silver" => "TechMember Silver 🥈",
        _ => "TechMember Standard ⭐"
    };

    public decimal NextTierThreshold => VipTier switch
    {
        "Member" => 10_000_000m,
        "Silver" => 30_000_000m,
        "Gold" => 70_000_000m,
        _ => 70_000_000m
    };

    public string NextTierName => VipTier switch
    {
        "Member" => "Hạng Bạc (Silver)",
        "Silver" => "Hạng Vàng (Gold)",
        "Gold" => "Hạng Kim Cương (Diamond)",
        _ => "Hạng Cao Nhất (VIP Max)"
    };

    public int TierProgressPercent => VipTier switch
    {
        "Diamond" => 100,
        "Gold" => (int)Math.Min(100, Math.Max(0, (TotalSpent - 30_000_000m) / 40_000_000m * 100)),
        "Silver" => (int)Math.Min(100, Math.Max(0, (TotalSpent - 10_000_000m) / 20_000_000m * 100)),
        _ => (int)Math.Min(100, Math.Max(0, TotalSpent / 10_000_000m * 100))
    };

    public decimal AmountNeededForNextTier => VipTier == "Diamond" ? 0m : Math.Max(0m, NextTierThreshold - TotalSpent);
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

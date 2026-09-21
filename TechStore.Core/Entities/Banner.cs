using System;

namespace TechStore.Core.Entities;

/// <summary>
/// Bảng quản lý Banner quảng cáo CMS đa vị trí
/// </summary>
public class Banner
{
    public int BannerId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Subtitle { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string? MobileImageUrl { get; set; }
    public string? TargetUrl { get; set; }
    public string Position { get; set; } = "HomeHeroSlider"; // 'HomeHeroSlider', 'HomeSubBanner', 'PromoPopup'
    public int DisplayOrder { get; set; } = 0;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public int? CreatedBy { get; set; }
}
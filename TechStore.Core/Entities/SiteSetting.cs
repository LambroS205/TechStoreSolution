using System;

namespace TechStore.Core.Entities;

/// <summary>
/// Đại diện cho cấu hình hệ thống động (Logo, Màu sắc, VietQR, Footer, SEO, CSS tùy biến)
/// </summary>
public class SiteSetting
{
    public int SettingId { get; set; }
    public string Key { get; set; } = string.Empty;
    public string? Value { get; set; }
    public string Group { get; set; } = "General"; // 'General', 'Branding', 'VietQR', 'Footer', 'CustomCss'
    public string? Description { get; set; }
    public string ValueType { get; set; } = "text"; // 'text', 'textarea', 'color', 'image', 'code', 'boolean'
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string? UpdatedBy { get; set; }
}

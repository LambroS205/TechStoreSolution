using System.Collections.Generic;
using System.Threading.Tasks;
using TechStore.Core.Entities;

namespace TechStore.Core.Interfaces;

/// <summary>
/// Giao diện dịch vụ quản lý cấu hình giao diện và hệ thống động (Site Settings)
/// </summary>
public interface ISiteSettingsService
{
    /// <summary>
    /// Lấy giá trị cấu hình theo Key (có fallback defaultValue)
    /// </summary>
    Task<string?> GetValueAsync(string key, string? defaultValue = null);

    /// <summary>
    /// Lấy toàn bộ cấu hình dưới dạng Dictionary Key-Value (tối ưu hiệu năng từ Cache)
    /// </summary>
    Task<Dictionary<string, string>> GetAllSettingsAsync();

    /// <summary>
    /// Lấy cấu hình theo nhóm (General, Branding, VietQR, Footer, CustomCss)
    /// </summary>
    Task<Dictionary<string, string>> GetSettingsByGroupAsync(string group);

    /// <summary>
    /// Lấy danh sách các Entity cấu hình đầy đủ (phục vụ Admin CMS)
    /// </summary>
    Task<List<SiteSetting>> GetAllSettingEntitiesAsync();

    /// <summary>
    /// Cập nhật giá trị một thiết lập
    /// </summary>
    Task<bool> UpdateSettingAsync(string key, string? value, string? updatedBy = null);

    /// <summary>
    /// Cập nhật hàng loạt thiết lập và làm mới bộ nhớ đệm
    /// </summary>
    Task<bool> UpdateSettingsBatchAsync(IDictionary<string, string?> settings, string? updatedBy = null);

    /// <summary>
    /// Xóa bộ nhớ đệm cấu hình để tải lại dữ liệu mới nhất từ CSDL
    /// </summary>
    void InvalidateCache();
}

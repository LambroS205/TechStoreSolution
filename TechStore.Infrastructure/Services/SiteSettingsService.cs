using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using TechStore.Core.Entities;
using TechStore.Core.Interfaces;
using TechStore.Infrastructure.Data;

namespace TechStore.Infrastructure.Services;

/// <summary>
/// Dịch vụ quản lý các thiết lập động của hệ thống (Logo, màu sắc, VietQR, Footer, SEO, CSS)
/// Tích hợp bộ nhớ đệm IMemoryCache tối ưu tốc độ truy xuất trên Storefront
/// </summary>
public class SiteSettingsService : ISiteSettingsService
{
    private readonly TechStoreDbContext _context;
    private readonly IMemoryCache _cache;
    private readonly ILogger<SiteSettingsService> _logger;

    private const string CacheKey = "SiteSettings_AllDictionary";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);

    public SiteSettingsService(
        TechStoreDbContext context,
        IMemoryCache cache,
        ILogger<SiteSettingsService> logger)
    {
        _context = context;
        _cache = cache;
        _logger = logger;
    }

    public async Task<string?> GetValueAsync(string key, string? defaultValue = null)
    {
        var settings = await GetAllSettingsAsync();
        if (settings.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        return defaultValue;
    }

    public async Task<Dictionary<string, string>> GetAllSettingsAsync()
    {
        return await _cache.GetOrCreateAsync(CacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;
            entry.Priority = CacheItemPriority.High;

            try
            {
                var list = await _context.SiteSettings
                    .AsNoTracking()
                    .ToListAsync();

                return list.ToDictionary(
                    s => s.Key,
                    s => s.Value ?? string.Empty,
                    StringComparer.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi truy vấn danh sách SiteSettings từ CSDL");
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }
        }) ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }

    public async Task<Dictionary<string, string>> GetSettingsByGroupAsync(string group)
    {
        try
        {
            var list = await _context.SiteSettings
                .AsNoTracking()
                .Where(s => s.Group.ToLower() == group.ToLower())
                .ToListAsync();

            return list.ToDictionary(
                s => s.Key,
                s => s.Value ?? string.Empty,
                StringComparer.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi truy vấn SiteSettings theo Group {Group}", group);
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }

    public async Task<List<SiteSetting>> GetAllSettingEntitiesAsync()
    {
        try
        {
            return await _context.SiteSettings
                .AsNoTracking()
                .OrderBy(s => s.Group)
                .ThenBy(s => s.SettingId)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi truy vấn GetAllSettingEntitiesAsync");
            return new List<SiteSetting>();
        }
    }

    public async Task<bool> UpdateSettingAsync(string key, string? value, string? updatedBy = null)
    {
        try
        {
            var setting = await _context.SiteSettings.FirstOrDefaultAsync(s => s.Key == key);
            if (setting != null)
            {
                setting.Value = value;
                setting.UpdatedAt = DateTime.UtcNow;
                setting.UpdatedBy = updatedBy;
            }
            else
            {
                _context.SiteSettings.Add(new SiteSetting
                {
                    Key = key,
                    Value = value,
                    Group = "General",
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedBy = updatedBy
                });
            }

            await _context.SaveChangesAsync();
            InvalidateCache();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi cập nhật thiết lập {Key}", key);
            return false;
        }
    }

    public async Task<bool> UpdateSettingsBatchAsync(IDictionary<string, string?> settings, string? updatedBy = null)
    {
        try
        {
            var existingSettings = await _context.SiteSettings.ToListAsync();
            var existingDict = existingSettings.ToDictionary(s => s.Key, StringComparer.OrdinalIgnoreCase);

            foreach (var kvp in settings)
            {
                if (existingDict.TryGetValue(kvp.Key, out var existing))
                {
                    existing.Value = kvp.Value;
                    existing.UpdatedAt = DateTime.UtcNow;
                    existing.UpdatedBy = updatedBy;
                }
                else
                {
                    _context.SiteSettings.Add(new SiteSetting
                    {
                        Key = kvp.Key,
                        Value = kvp.Value,
                        Group = DetermineGroup(kvp.Key),
                        UpdatedAt = DateTime.UtcNow,
                        UpdatedBy = updatedBy
                    });
                }
            }

            await _context.SaveChangesAsync();
            InvalidateCache();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi cập nhật hàng loạt SiteSettings");
            return false;
        }
    }

    public void InvalidateCache()
    {
        _cache.Remove(CacheKey);
        _logger.LogInformation("Bộ nhớ đệm SiteSettings đã được xóa thành công");
    }

    private static string DetermineGroup(string key)
    {
        if (key.StartsWith("Brand.") || key.StartsWith("Theme.")) return "Branding";
        if (key.StartsWith("VietQr.")) return "VietQR";
        if (key.StartsWith("Footer.")) return "Footer";
        if (key.StartsWith("CustomCss") || key.StartsWith("CustomJs")) return "CustomCss";
        return "General";
    }
}

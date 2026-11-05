using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using TechStore.Core.Interfaces;

namespace TechStore.Infrastructure.Services;

/// <summary>
/// Dịch vụ làm mới và xóa bộ nhớ đệm IMemoryCache an toàn
/// </summary>
public class CacheInvalidationService : ICacheInvalidationService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<CacheInvalidationService> _logger;

    public CacheInvalidationService(IMemoryCache cache, ILogger<CacheInvalidationService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public void InvalidateHomePageCache()
    {
        _cache.Remove("home_page_viewmodel");
        _logger.LogInformation("[Cache] Đã làm mới cache trang chủ (home_page_viewmodel)");
    }

    public void InvalidateCategoriesCache()
    {
        _cache.Remove("all_active_categories");
        _logger.LogInformation("[Cache] Đã làm mới cache danh mục (all_active_categories)");
    }

    public void InvalidateBrandsCache()
    {
        _cache.Remove("all_active_brands");
        _logger.LogInformation("[Cache] Đã làm mới cache thương hiệu (all_active_brands)");
    }

    public void InvalidateUserPermissionsCache(int userId)
    {
        _cache.Remove($"UserPermissions_{userId}");
        _logger.LogInformation("[Cache] Đã làm mới cache quyền hạn người dùng (UserPermissions_{UserId})", userId);
    }

    public void InvalidateAllProductCaches()
    {
        InvalidateHomePageCache();
        InvalidateCategoriesCache();
        InvalidateBrandsCache();
    }
}


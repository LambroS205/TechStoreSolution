namespace TechStore.Core.Interfaces;

/// <summary>
/// Giao diện dịch vụ quản lý và giải phóng bộ nhớ đệm (Cache Invalidation) tập trung
/// Giúp Storefront luôn hiển thị dữ liệu mới nhất khi Admin thao tác
/// </summary>
public interface ICacheInvalidationService
{
    void InvalidateHomePageCache();
    void InvalidateCategoriesCache();
    void InvalidateBrandsCache();
    void InvalidateUserPermissionsCache(int userId);
    void InvalidateAllProductCaches();
}


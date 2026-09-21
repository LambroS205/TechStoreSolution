using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using TechStore.Infrastructure.Data;

namespace TechStore.Web.Security;

/// <summary>
/// Authorization Handler: Xử lý logic kiểm tra quyền của tài khoản kết hợp bộ nhớ đệm IMemoryCache
/// </summary>
public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IMemoryCache _cache;

    public PermissionAuthorizationHandler(IServiceScopeFactory scopeFactory, IMemoryCache cache)
    {
        _scopeFactory = scopeFactory;
        _cache = cache;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (!context.User.Identity?.IsAuthenticated ?? true)
        {
            return;
        }

        // 1. SuperAdmin luôn có toàn quyền (Bypass check)
        if (context.User.IsInRole("SuperAdmin"))
        {
            context.Succeed(requirement);
            return;
        }

        // 2. Lấy UserId từ Claims
        var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
        {
            return;
        }

        // 3. Truy vấn tập quyền hợp lệ của User (Sử dụng Memory Cache 5 phút)
        string cacheKey = $"UserPermissions_{userId}";
        var userPermissions = await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);

            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<TechStoreDbContext>();

            // Lấy các quyền từ Roles của User
            var rolePermissions = await dbContext.UserRoles
                .AsNoTracking()
                .Where(ur => ur.UserId == userId)
                .SelectMany(ur => ur.Role.RolePermissions.Select(rp => rp.Permission.PermissionCode))
                .ToListAsync();

            // Lấy quyền đặc cách cá nhân (Whitelist & Blacklist)
            var customPermissions = await dbContext.UserCustomPermissions
                .AsNoTracking()
                .Where(ucp => ucp.UserId == userId)
                .Select(ucp => new { ucp.Permission.PermissionCode, ucp.IsGranted })
                .ToListAsync();

            var effectivePermissions = new HashSet<string>(rolePermissions, StringComparer.OrdinalIgnoreCase);

            // Áp dụng đặc cách
            foreach (var cp in customPermissions)
            {
                if (cp.IsGranted)
                {
                    effectivePermissions.Add(cp.PermissionCode); // Cấp thêm
                }
                else
                {
                    effectivePermissions.Remove(cp.PermissionCode); // Tước quyền (Blacklist)
                }
            }

            return effectivePermissions;
        });

        // 4. Kiểm tra mã quyền
        if (userPermissions != null && userPermissions.Contains(requirement.PermissionCode))
        {
            context.Succeed(requirement);
        }
    }
}
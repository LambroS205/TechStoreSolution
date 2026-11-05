using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechStore.Core.Entities;
using TechStore.Core.Interfaces;
using TechStore.Infrastructure.Data;
using TechStore.Web.Areas.Admin.Models;
using TechStore.Web.Security;

namespace TechStore.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
public class UserController : Controller
{
    private readonly TechStoreDbContext _context;
    private readonly IAuditLogService _auditLogService;
    private readonly ICacheInvalidationService _cacheInvalidation;
    private readonly PasswordHasher<User> _passwordHasher;

    public UserController(TechStoreDbContext context, IAuditLogService auditLogService, ICacheInvalidationService cacheInvalidation)
    {
        _context = context;
        _cacheInvalidation = cacheInvalidation;
        _auditLogService = auditLogService;
        _passwordHasher = new PasswordHasher<User>();
    }

    [HttpGet]
    [HasPermission("Users.View")]
    public async Task<IActionResult> Index(string? search, string? role, string? tab = "all", int page = 1, int pageSize = 15)
    {
        if (page < 1) page = 1;

        var baseQuery = _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .AsNoTracking();

        // Thống kê toàn hệ thống
        var totalUsers = await _context.Users.CountAsync();
        var customerCount = await _context.Users.CountAsync(u => u.UserRoles.Any(r => r.Role.NormalizedName == "CUSTOMER"));
        var staffCount = await _context.Users.CountAsync(u => u.UserRoles.Any(r => r.Role.NormalizedName != "CUSTOMER"));
        var lockedCount = await _context.Users.CountAsync(u => !u.IsActive || (u.LockoutEnd != null && u.LockoutEnd > DateTime.UtcNow));

        var query = baseQuery;

        // Phân loại theo Tab
        string currentTab = (tab ?? "all").ToLowerInvariant();
        if (currentTab == "customers")
        {
            query = query.Where(u => u.UserRoles.Any(r => r.Role.NormalizedName == "CUSTOMER"));
        }
        else if (currentTab == "staff")
        {
            query = query.Where(u => u.UserRoles.Any(r => r.Role.NormalizedName != "CUSTOMER"));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.Trim().ToLower();
            query = query.Where(u => u.Username.ToLower().Contains(searchLower)
                                  || u.Email.ToLower().Contains(searchLower)
                                  || u.FullName.ToLower().Contains(searchLower)
                                  || (u.PhoneNumber != null && u.PhoneNumber.Contains(searchLower)));
        }

        if (!string.IsNullOrWhiteSpace(role))
        {
            query = query.Where(u => u.UserRoles.Any(ur => ur.Role.NormalizedName == role.ToUpper() || ur.Role.RoleName == role));
        }

        var totalFiltered = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalFiltered / (double)pageSize);
        if (totalPages < 1) totalPages = 1;
        if (page > totalPages) page = totalPages;

        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var availableRoles = await _context.Roles.AsNoTracking().OrderBy(r => r.RoleName).ToListAsync();

        var model = new UserListViewModel
        {
            Users = users,
            TotalUsers = totalUsers,
            CustomerCount = customerCount,
            StaffCount = staffCount,
            LockedCount = lockedCount,
            CurrentTab = currentTab,
            CurrentSearch = search,
            CurrentRole = role,
            PageNumber = page,
            PageSize = pageSize,
            TotalPages = totalPages,
            AvailableRoles = availableRoles
        };

        return View(model);
    }

    [HttpGet]
    [HasPermission("Users.View")]
    public async Task<IActionResult> Detail(int id)
    {
        var user = await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .Include(u => u.Addresses)
            .Include(u => u.Wishlists)
                .ThenInclude(w => w.Product)
            .Include(u => u.Reviews)
                .ThenInclude(r => r.Product)
            .FirstOrDefaultAsync(u => u.UserId == id);

        if (user == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy người dùng với mã đã chỉ định.";
            return RedirectToAction(nameof(Index));
        }

        bool isStaff = user.IsStaffOrAdmin();
        int auditLogsCount = 0;
        int ordersHandledCount = 0;
        int inventoryTxCount = 0;
        int blogPostsCount = 0;
        var permissionsByModule = new Dictionary<string, List<Permission>>();
        var recentAuditLogs = new List<AuditLog>();

        // Chỉ nạp dữ liệu mua sắm nếu là Khách hàng
        var orders = new List<Order>();
        decimal totalSpent = 0;
        int completedOrders = 0;
        int cancelledOrders = 0;

        if (isStaff)
        {
            auditLogsCount = await _context.AuditLogs.AsNoTracking().CountAsync(a => a.UserId == id);
            ordersHandledCount = await _context.OrderStatusHistories.AsNoTracking().CountAsync(h => h.ChangedBy == id);
            inventoryTxCount = await _context.InventoryTransactions.AsNoTracking().CountAsync(t => t.CreatedBy == id);
            blogPostsCount = await _context.BlogPosts.AsNoTracking().CountAsync(b => b.AuthorId == id);
            recentAuditLogs = await _context.AuditLogs.AsNoTracking().Where(a => a.UserId == id).OrderByDescending(a => a.CreatedAt).Take(15).ToListAsync();

            bool isSuperAdmin = user.UserRoles.Any(ur => ur.Role.NormalizedName == "SUPERADMIN");
            var allPerms = await _context.Permissions.AsNoTracking().OrderBy(p => p.Module).ThenBy(p => p.Action).ToListAsync();
            if (isSuperAdmin)
            {
                permissionsByModule = allPerms.GroupBy(p => p.Module ?? "Chung").ToDictionary(g => g.Key, g => g.ToList());
            }
            else
            {
                var roleIds = user.UserRoles.Select(ur => ur.RoleId).ToList();
                var inheritedPermIds = await _context.RolePermissions
                    .AsNoTracking()
                    .Where(rp => roleIds.Contains(rp.RoleId))
                    .Select(rp => rp.PermissionId)
                    .ToListAsync();

                var customPerms = await _context.UserCustomPermissions
                    .AsNoTracking()
                    .Where(cp => cp.UserId == id)
                    .ToListAsync();

                var grantedCustom = customPerms.Where(cp => cp.IsGranted).Select(cp => cp.PermissionId).ToHashSet();
                var deniedCustom = customPerms.Where(cp => !cp.IsGranted).Select(cp => cp.PermissionId).ToHashSet();

                var effectivePermIds = inheritedPermIds.Union(grantedCustom).Except(deniedCustom).ToHashSet();
                var userPerms = allPerms.Where(p => effectivePermIds.Contains(p.PermissionId)).ToList();
                permissionsByModule = userPerms.GroupBy(p => p.Module ?? "Chung").ToDictionary(g => g.Key, g => g.ToList());
            }
        }
        else
        {
            orders = await _context.Orders
                .Include(o => o.OrderDetails)
                .Include(o => o.PaymentTransactions)
                .Where(o => o.UserId == id || (user.PhoneNumber != null && o.CustomerPhone == user.PhoneNumber) || (user.Email != null && o.CustomerEmail == user.Email))
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            totalSpent = orders
                .Where(o => o.OrderStatus == "Delivered" || o.PaymentStatus == "Paid")
                .Sum(o => o.TotalAmount);

            completedOrders = orders.Count(o => o.OrderStatus == "Delivered");
            cancelledOrders = orders.Count(o => o.OrderStatus == "Cancelled");
        }

        var allRoles = await _context.Roles.AsNoTracking().OrderBy(r => r.RoleName).ToListAsync();
        var userRoleIds = user.UserRoles.Select(ur => ur.RoleId).ToList();

        var model = new UserDetailViewModel
        {
            User = user,
            IsStaffAccount = isStaff,
            AuditLogsCount = auditLogsCount,
            OrdersHandledCount = ordersHandledCount,
            InventoryTxCount = inventoryTxCount,
            BlogPostsCount = blogPostsCount,
            EffectivePermissionsByModule = permissionsByModule,
            RecentAuditLogs = recentAuditLogs,
            Orders = orders,
            TotalSpent = totalSpent,
            TotalOrders = orders.Count,
            CompletedOrders = completedOrders,
            CancelledOrders = cancelledOrders,
            AllRoles = allRoles,
            UserRoleIds = userRoleIds
        };

        return View(model);
    }

    /// <summary>
    /// Điều hướng an toàn khi truy cập trực tiếp bằng phương thức GET (tránh lỗi 405 Method Not Allowed)
    /// </summary>
    [HttpGet]
    [Route("Admin/User/ToggleLock/{id?}")]
    [Route("Admin/User/ResetPassword/{id?}")]
    [Route("Admin/User/UpdateRoles/{id?}")]
    public IActionResult SafeUserPostRedirect(int? id) => id.HasValue ? RedirectToAction(nameof(Detail), new { id }) : RedirectToAction(nameof(Index));

    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission("Users.Edit")]
    [HttpGet]
    public IActionResult ToggleLock() => RedirectToAction(nameof(Index));

    [HttpGet]
    public IActionResult ResetPassword() => RedirectToAction(nameof(Index));

    [HttpGet]
    public IActionResult UpdateRoles() => RedirectToAction(nameof(Index));

    public async Task<IActionResult> ToggleLock(int id)
    {
        var currentUserIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (int.TryParse(currentUserIdStr, out var currentUserId) && currentUserId == id)
        {
            TempData["ErrorMessage"] = "Bạn không thể tự khóa tài khoản quản trị đang đăng nhập.";
            return RedirectToAction(nameof(Detail), new { id });
        }

        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy người dùng.";
            return RedirectToAction(nameof(Index));
        }

        var oldStatus = user.IsActive;
        user.IsActive = !user.IsActive;
        if (!user.IsActive)
        {
            user.LockoutEnd = DateTime.UtcNow.AddYears(10);
        }
        else
        {
            user.LockoutEnd = null;
            user.AccessFailedCount = 0;
        }
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        _cacheInvalidation.InvalidateUserPermissionsCache(id);

        await _auditLogService.LogAsync(
            action: user.IsActive ? "UnlockUser" : "LockUser",
            module: "Users",
            recordId: user.UserId.ToString(),
            oldValues: $"IsActive: {oldStatus}",
            newValues: $"IsActive: {user.IsActive}"
        );

        TempData["SuccessMessage"] = user.IsActive
            ? $"Đã mở khóa tài khoản '{user.Username}' thành công."
            : $"Đã khóa tài khoản '{user.Username}' thành công.";

        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission("Users.Edit")]
    public async Task<IActionResult> ResetPassword(int id, string newPassword)
    {
        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 8)
        {
            TempData["ErrorMessage"] = "Mật khẩu mới phải có tối thiểu 8 ký tự.";
            return RedirectToAction(nameof(Detail), new { id });
        }

        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy người dùng.";
            return RedirectToAction(nameof(Index));
        }

        user.PasswordHash = _passwordHasher.HashPassword(user, newPassword);
        user.SecurityStamp = Guid.NewGuid().ToString();
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        _cacheInvalidation.InvalidateUserPermissionsCache(id);

        await _auditLogService.LogAsync(
            action: "ResetPassword",
            module: "Users",
            recordId: user.UserId.ToString(),
            oldValues: null,
            newValues: "Password reset by admin"
        );

        TempData["SuccessMessage"] = $"Đã đặt lại mật khẩu mới cho tài khoản '{user.Username}' thành công.";
        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission("Users.Edit")]
    public async Task<IActionResult> UpdateRoles(int id, List<int> roleIds)
    {
        var user = await _context.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.UserId == id);

        if (user == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy người dùng.";
            return RedirectToAction(nameof(Index));
        }

        var currentUserIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isSelf = int.TryParse(currentUserIdStr, out var currentUserId) && currentUserId == id;

        roleIds ??= new List<int>();

        // Giữ lại vai trò SuperAdmin cho chính tài khoản SuperAdmin đang thao tác để tránh bị tự tước quyền
        var superAdminRole = await _context.Roles.FirstOrDefaultAsync(r => r.NormalizedName == "SUPERADMIN");
        if (isSelf && superAdminRole != null && user.UserRoles.Any(ur => ur.RoleId == superAdminRole.RoleId))
        {
            if (!roleIds.Contains(superAdminRole.RoleId))
            {
                roleIds.Add(superAdminRole.RoleId);
            }
        }

        var oldRoles = string.Join(",", user.UserRoles.Select(ur => ur.RoleId));

        _context.UserRoles.RemoveRange(user.UserRoles);
        foreach (var rId in roleIds)
        {
            _context.UserRoles.Add(new UserRole { UserId = id, RoleId = rId });
        }

        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        _cacheInvalidation.InvalidateUserPermissionsCache(id);

        var newRoles = string.Join(",", roleIds);

        await _auditLogService.LogAsync(
            action: "UpdateRoles",
            module: "Users",
            recordId: user.UserId.ToString(),
            oldValues: $"RoleIds: {oldRoles}",
            newValues: $"RoleIds: {newRoles}"
        );

        TempData["SuccessMessage"] = $"Đã cập nhật danh sách vai trò cho '{user.Username}' thành công.";
        return RedirectToAction(nameof(Detail), new { id });
    }
}

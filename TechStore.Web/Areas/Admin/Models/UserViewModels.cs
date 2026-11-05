using System.Collections.Generic;
using TechStore.Core.Entities;

namespace TechStore.Web.Areas.Admin.Models;

public class UserListViewModel
{
    public List<User> Users { get; set; } = new();
    public int TotalUsers { get; set; }
    public int CustomerCount { get; set; }
    public int StaffCount { get; set; }
    public int LockedCount { get; set; }
    public string CurrentTab { get; set; } = "all";
    public string? CurrentSearch { get; set; }
    public string? CurrentRole { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 15;
    public int TotalPages { get; set; } = 1;
    public List<Role> AvailableRoles { get; set; } = new();
}

public class UserDetailViewModel
{
    public User User { get; set; } = null!;
    public bool IsStaffAccount { get; set; }
    public string EmployeeCode => $"#EMP-{User?.UserId:D4}";

    // Chỉ số vận hành cho nhân viên
    public int AuditLogsCount { get; set; }
    public int OrdersHandledCount { get; set; }
    public int InventoryTxCount { get; set; }
    public int BlogPostsCount { get; set; }
    public Dictionary<string, List<Permission>> EffectivePermissionsByModule { get; set; } = new();
    public List<AuditLog> RecentAuditLogs { get; set; } = new();

    // Dữ liệu mua sắm cho khách hàng
    public List<Order> Orders { get; set; } = new();
    public decimal TotalSpent { get; set; }
    public int TotalOrders { get; set; }
    public int CompletedOrders { get; set; }
    public int CancelledOrders { get; set; }

    // Vai trò & phân quyền
    public List<Role> AllRoles { get; set; } = new();
    public List<int> UserRoleIds { get; set; } = new();
}

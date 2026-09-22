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
    public List<Order> Orders { get; set; } = new();
    public decimal TotalSpent { get; set; }
    public int TotalOrders { get; set; }
    public int CompletedOrders { get; set; }
    public int CancelledOrders { get; set; }
    public List<Role> AllRoles { get; set; } = new();
    public List<int> UserRoleIds { get; set; } = new();
}

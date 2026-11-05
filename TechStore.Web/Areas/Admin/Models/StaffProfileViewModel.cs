using System;
using System.Collections.Generic;
using TechStore.Core.Entities;

namespace TechStore.Web.Areas.Admin.Models;

public class StaffProfileViewModel
{
    public int UserId { get; set; }
    public string EmployeeCode => $"#EMP-{UserId:D4}";
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? AvatarUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<string> Roles { get; set; } = new();
    public bool IsSuperAdmin { get; set; }

    // Chỉ số hiệu suất nghiệp vụ (Operational KPIs)
    public int AuditActionsCount { get; set; }
    public int OrdersHandledCount { get; set; }
    public int InventoryTxCount { get; set; }
    public int BlogPostsCount { get; set; }

    // Danh mục quyền hạn theo phân hệ
    public Dictionary<string, List<Permission>> EffectivePermissionsByModule { get; set; } = new();

    // Nhật ký hoạt động quản trị gần nhất
    public List<AuditLog> RecentAuditLogs { get; set; } = new();
}

public class StaffProfileUpdateDto
{
    public string FullName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
}

public class StaffChangePasswordDto
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}


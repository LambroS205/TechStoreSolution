using System.Collections.Generic;

namespace TechStore.Core.Entities;

/// <summary>
/// Danh mục chi tiết từng quyền hạn trong hệ thống theo Module và Action
/// </summary>
public class Permission
{
    public int PermissionId { get; set; }
    public string Module { get; set; } = string.Empty;        // 'Products', 'Orders', 'Banners', 'Inventory'
    public string Action { get; set; } = string.Empty;        // 'View', 'Create', 'Edit', 'Delete', 'Approve'
    public string PermissionCode { get; set; } = string.Empty; // 'Products.View', 'Orders.ApprovePayment'
    public string Description { get; set; } = string.Empty;

    // Navigation Properties
    public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    public virtual ICollection<UserCustomPermission> UserCustomPermissions { get; set; } = new List<UserCustomPermission>();
}
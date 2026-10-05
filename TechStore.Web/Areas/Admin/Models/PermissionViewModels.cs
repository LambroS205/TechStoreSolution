using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using TechStore.Core.Entities;

namespace TechStore.Web.Areas.Admin.Models;

/// <summary>
/// ViewModel phục vụ ma trận phân quyền RBAC đa cấp (theo Vai trò và theo Người dùng cụ thể)
/// </summary>
public class PermissionMatrixViewModel
{
    public string ActiveTab { get; set; } = "role"; // "role" hoặc "user"
    public List<Role> Roles { get; set; } = new();
    public int CurrentRoleId { get; set; }
    public Role? CurrentRole { get; set; }

    public List<User> SubAdmins { get; set; } = new();
    public int CurrentUserId { get; set; }
    public User? CurrentUser { get; set; }

    public Dictionary<string, List<Permission>> GroupedPermissions { get; set; } = new();
    public HashSet<int> AssignedPermissionIds { get; set; } = new();

    public HashSet<int> UserInheritedRolePermissionIds { get; set; } = new();
    public HashSet<int> UserGrantedCustomPermissionIds { get; set; } = new();
    public HashSet<int> UserDeniedCustomPermissionIds { get; set; } = new();
}

public class SaveRolePermissionsDto
{
    public int RoleId { get; set; }
    public List<int> PermissionIds { get; set; } = new();
}

public class SaveUserCustomPermissionsDto
{
    public int UserId { get; set; }
    public List<int> GrantedPermissionIds { get; set; } = new();
    public List<int> DeniedPermissionIds { get; set; } = new();
}

public class CreateSubAdminDto
{
    [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập họ và tên")]
    public string FullName { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Email không đúng định dạng")]
    public string? Email { get; set; }

    public string? PhoneNumber { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu khởi tạo")]
    [MinLength(8, ErrorMessage = "Mật khẩu tối thiểu 8 ký tự")]
    public string Password { get; set; } = string.Empty;

    public int RoleId { get; set; }
}

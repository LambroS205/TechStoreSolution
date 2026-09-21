using Microsoft.AspNetCore.Authorization;

namespace TechStore.Web.Security;

/// <summary>
/// Requirement chứa mã quyền cần kiểm tra
/// </summary>
public class PermissionRequirement : IAuthorizationRequirement
{
    public string PermissionCode { get; }

    public PermissionRequirement(string permissionCode)
    {
        PermissionCode = permissionCode;
    }
}
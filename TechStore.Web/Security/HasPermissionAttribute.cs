using System;
using Microsoft.AspNetCore.Authorization;

namespace TechStore.Web.Security;

/// <summary>
/// Custom Attribute dùng trên Action hoặc Controller: [HasPermission("Products.Edit")]
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public class HasPermissionAttribute : AuthorizeAttribute
{
    public const string PolicyPrefix = "Permission:";

    public HasPermissionAttribute(string permissionCode)
    {
        Policy = $"{PolicyPrefix}{permissionCode}";
    }
}
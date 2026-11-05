using System;
using System.Linq;
using System.Security.Claims;
using TechStore.Core.Entities;

namespace TechStore.Web.Security;

public static class UserRoleExtensions
{
    public static bool IsStaffOrAdmin(this ClaimsPrincipal? principal)
    {
        if (principal?.Identity?.IsAuthenticated != true) return false;
        return principal.Claims.Any(c =>
            c.Type == ClaimTypes.Role &&
            !string.Equals(c.Value, "Customer", StringComparison.OrdinalIgnoreCase));
    }

    public static bool IsStaffOrAdmin(this User? user)
    {
        if (user?.UserRoles == null || !user.UserRoles.Any()) return false;
        return user.UserRoles.Any(ur =>
            ur.Role != null &&
            !string.Equals(ur.Role.NormalizedName, "CUSTOMER", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(ur.Role.RoleName, "Customer", StringComparison.OrdinalIgnoreCase));
    }
}


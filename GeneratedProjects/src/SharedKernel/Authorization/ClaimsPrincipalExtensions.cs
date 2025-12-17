using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace TestAttarClone.SharedKernel.Authorization;

/// <summary>
/// Extension methods for ClaimsPrincipal to check permissions
/// </summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Checks if the user has a specific permission
    /// </summary>
    /// <param name="principal">The claims principal</param>
    /// <param name="permission">The permission key to check</param>
    /// <returns>True if the user has the permission or is an Admin; otherwise false</returns>
    public static bool HasPermission(this ClaimsPrincipal principal, string permission)
    {
        if (principal?.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(permission))
        {
            return false;
        }

        // Admin users have all permissions
        if (principal.IsInRole(RoleNames.Admin))
        {
            return true;
        }

        return principal.HasClaim(claim =>
            string.Equals(claim.Type, PermissionCatalog.ClaimType, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(claim.Value, permission, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Checks if the user has all of the specified permissions
    /// </summary>
    /// <param name="principal">The claims principal</param>
    /// <param name="permissions">The permission keys to check</param>
    /// <returns>True if the user has all permissions or is an Admin; otherwise false</returns>
    public static bool HasAllPermissions(this ClaimsPrincipal principal, params string[] permissions)
    {
        if (principal?.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        if (permissions is null || permissions.Length == 0)
        {
            return false;
        }

        // Admin users have all permissions
        if (principal.IsInRole(RoleNames.Admin))
        {
            return true;
        }

        var userPermissions = principal.Claims
            .Where(claim => string.Equals(claim.Type, PermissionCatalog.ClaimType, StringComparison.OrdinalIgnoreCase))
            .Select(claim => claim.Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return permissions.All(permission =>
            !string.IsNullOrWhiteSpace(permission) &&
            userPermissions.Contains(permission));
    }

    /// <summary>
    /// Checks if the user has any of the specified permissions
    /// </summary>
    /// <param name="principal">The claims principal</param>
    /// <param name="permissions">The permission keys to check</param>
    /// <returns>True if the user has at least one permission or is an Admin; otherwise false</returns>
    public static bool HasAnyPermission(this ClaimsPrincipal principal, params string[] permissions)
    {
        if (principal?.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        if (permissions is null || permissions.Length == 0)
        {
            return false;
        }

        // Admin users have all permissions
        if (principal.IsInRole(RoleNames.Admin))
        {
            return true;
        }

        var userPermissions = principal.Claims
            .Where(claim => string.Equals(claim.Type, PermissionCatalog.ClaimType, StringComparison.OrdinalIgnoreCase))
            .Select(claim => claim.Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return permissions.Any(permission =>
            !string.IsNullOrWhiteSpace(permission) &&
            userPermissions.Contains(permission));
    }

    /// <summary>
    /// Gets all permissions for the current user
    /// </summary>
    /// <param name="principal">The claims principal</param>
    /// <returns>Collection of permission keys</returns>
    public static IReadOnlyCollection<string> GetPermissions(this ClaimsPrincipal principal)
    {
        if (principal?.Identity?.IsAuthenticated != true)
        {
            return Array.Empty<string>();
        }

        return principal.Claims
            .Where(claim => string.Equals(claim.Type, PermissionCatalog.ClaimType, StringComparison.OrdinalIgnoreCase))
            .Select(claim => claim.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}

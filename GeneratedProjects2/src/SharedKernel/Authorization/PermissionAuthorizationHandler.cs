using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace LogsDtoCloneTest.SharedKernel.Authorization;

/// <summary>
/// Authorization handler for permission-based authorization
/// </summary>
public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (context.User?.Identity?.IsAuthenticated != true)
        {
            return Task.CompletedTask;
        }

        // Admin users bypass all permission checks
        if (context.User.IsInRole(RoleNames.Admin))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // Get all permission claims
        var userPermissions = context.User.Claims
            .Where(claim => string.Equals(claim.Type, PermissionCatalog.ClaimType, StringComparison.OrdinalIgnoreCase))
            .Select(claim => claim.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (userPermissions.Count == 0)
        {
            return Task.CompletedTask;
        }

        bool hasAccess;
        if (requirement.RequireAll)
        {
            // All permissions must be present (AND logic)
            hasAccess = requirement.Permissions.All(permission =>
                userPermissions.Contains(permission));
        }
        else
        {
            // Any permission is sufficient (OR logic)
            hasAccess = requirement.Permissions.Any(permission =>
                userPermissions.Contains(permission));
        }

        if (hasAccess)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace MobiRooz.SharedKernel.Authorization;

/// <summary>
/// Custom policy provider for permission-based authorization
/// </summary>
public sealed class PermissionPolicyProvider : IAuthorizationPolicyProvider
{
    private const string PermissionPolicyPrefix = "Permission:";
    private readonly DefaultAuthorizationPolicyProvider _fallbackPolicyProvider;

    public PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
    {
        _fallbackPolicyProvider = new DefaultAuthorizationPolicyProvider(options);
    }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync()
        => _fallbackPolicyProvider.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync()
        => _fallbackPolicyProvider.GetFallbackPolicyAsync();

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (string.IsNullOrWhiteSpace(policyName))
        {
            return _fallbackPolicyProvider.GetPolicyAsync(policyName);
        }

        // Check if this is a permission policy
        if (policyName.StartsWith(PermissionPolicyPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var policy = ParsePermissionPolicy(policyName);
            if (policy is not null)
            {
                return Task.FromResult<AuthorizationPolicy?>(policy);
            }
        }

        // Fall back to default policy provider
        return _fallbackPolicyProvider.GetPolicyAsync(policyName);
    }

    private static AuthorizationPolicy? ParsePermissionPolicy(string policyName)
    {
        // Policy format: "Permission:{comma-separated-permissions}:{requireAll}"
        var parts = policyName.Substring(PermissionPolicyPrefix.Length).Split(':');
        if (parts.Length < 1)
        {
            return null;
        }

        var permissions = parts[0]
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(permission => !string.IsNullOrWhiteSpace(permission))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (permissions.Length == 0)
        {
            return null;
        }

        var requireAll = parts.Length > 1 &&
                        bool.TryParse(parts[1], out var requireAllValue) &&
                        requireAllValue;

        var policy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .AddRequirements(new PermissionRequirement(permissions, requireAll))
            .Build();

        return policy;
    }
}

using System;
using System.Linq;
using Microsoft.AspNetCore.Authorization;

namespace LogsDtoCloneTest.SharedKernel.Authorization;

/// <summary>
/// Specifies that the class or method requires one or more permissions
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class RequirePermissionAttribute : AuthorizeAttribute
{
    /// <summary>
    /// Gets the permission keys required
    /// </summary>
    public string[] Permissions { get; }

    /// <summary>
    /// Gets a value indicating whether all permissions are required
    /// </summary>
    public bool RequireAll { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="RequirePermissionAttribute"/> class
    /// </summary>
    /// <param name="permissions">The permission keys required (comma-separated or multiple parameters)</param>
    public RequirePermissionAttribute(params string[] permissions)
    {
        if (permissions is null || permissions.Length == 0)
        {
            throw new ArgumentException("At least one permission must be specified.", nameof(permissions));
        }

        // Handle comma-separated permissions in a single string
        Permissions = permissions
            .SelectMany(permission => permission.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Where(permission => !string.IsNullOrWhiteSpace(permission))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (Permissions.Length == 0)
        {
            throw new ArgumentException("At least one valid permission must be specified.", nameof(permissions));
        }

        // Set the policy name - this will be resolved by our policy provider
        Policy = $"Permission:{string.Join(",", Permissions)}:{RequireAll}";
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RequirePermissionAttribute"/> class with a single permission
    /// </summary>
    /// <param name="permission">The permission key required</param>
    public RequirePermissionAttribute(string permission) : this(new[] { permission })
    {
    }
}

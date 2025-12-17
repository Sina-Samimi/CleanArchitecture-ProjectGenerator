using Microsoft.AspNetCore.Authorization;

namespace LogsDtoCloneTest.SharedKernel.Authorization;

/// <summary>
/// Authorization requirement that requires one or more permissions
/// </summary>
public sealed class PermissionRequirement : IAuthorizationRequirement
{
    /// <summary>
    /// Gets the permission keys required
    /// </summary>
    public IReadOnlyCollection<string> Permissions { get; }

    /// <summary>
    /// Gets a value indicating whether all permissions are required (AND logic)
    /// or any permission is sufficient (OR logic)
    /// </summary>
    public bool RequireAll { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PermissionRequirement"/> class
    /// </summary>
    /// <param name="permissions">The permission keys required</param>
    /// <param name="requireAll">If true, all permissions must be present; if false, any permission is sufficient</param>
    public PermissionRequirement(IEnumerable<string> permissions, bool requireAll = false)
    {
        Permissions = permissions?.ToArray() ?? Array.Empty<string>();
        RequireAll = requireAll;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PermissionRequirement"/> class with a single permission
    /// </summary>
    /// <param name="permission">The permission key required</param>
    public PermissionRequirement(string permission)
    {
        Permissions = new[] { permission };
        RequireAll = false;
    }
}

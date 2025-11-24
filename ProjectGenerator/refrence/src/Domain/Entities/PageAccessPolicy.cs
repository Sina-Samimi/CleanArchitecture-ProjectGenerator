using System;
using System.Diagnostics.CodeAnalysis;
using Arsis.Domain.Base;

namespace Arsis.Domain.Entities;

public sealed class PageAccessPolicy : Entity
{
    public string Area { get; private set; } = string.Empty;

    public string Controller { get; private set; } = string.Empty;

    public string Action { get; private set; } = string.Empty;

    public string PermissionKey { get; private set; } = string.Empty;

    [SetsRequiredMembers]
    private PageAccessPolicy()
    {
    }

    [SetsRequiredMembers]
    public PageAccessPolicy(string area, string controller, string action, string permissionKey)
    {
        SetRoute(area, controller, action);
        UpdatePermission(permissionKey);
    }

    public void SetRoute(string area, string controller, string action)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(controller);
        ArgumentException.ThrowIfNullOrWhiteSpace(action);

        Area = string.IsNullOrWhiteSpace(area)
            ? string.Empty
            : area.Trim();
        Controller = controller.Trim();
        Action = action.Trim();
    }

    public void UpdatePermission(string permissionKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(permissionKey);
        PermissionKey = permissionKey.Trim();
    }
}

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace MobiRooz.SharedKernel.Authorization;

public static class PermissionCatalog
{
    public const string ClaimType = "permission";

    private static readonly IReadOnlyList<PermissionGroup> _groups =
        new ReadOnlyCollection<PermissionGroup>(Array.Empty<PermissionGroup>());

    private static readonly IReadOnlyDictionary<string, PermissionDefinition> _permissionsByKey =
        new ReadOnlyDictionary<string, PermissionDefinition>(
            new Dictionary<string, PermissionDefinition>(StringComparer.OrdinalIgnoreCase));

    public static IReadOnlyList<PermissionGroup> Groups => _groups;

    public static IReadOnlyDictionary<string, PermissionDefinition> Permissions => _permissionsByKey;

    public static bool IsValidPermission(string? key)
        => !string.IsNullOrWhiteSpace(key) && _permissionsByKey.ContainsKey(key);

    public static bool TryGetPermission(string key, out PermissionDefinition definition)
        => _permissionsByKey.TryGetValue(key, out definition!);

    public static PermissionDefinition? Find(string key)
        => _permissionsByKey.TryGetValue(key, out var definition) ? definition : null;
}

public sealed record PermissionGroup(string Key, string DisplayName, IReadOnlyCollection<PermissionDefinition> Permissions);

public sealed record PermissionDefinition(string Key, string DisplayName, string? Description);

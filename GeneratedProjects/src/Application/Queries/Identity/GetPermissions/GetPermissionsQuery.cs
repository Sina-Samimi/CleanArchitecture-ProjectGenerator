using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TestAttarClone.Application.Abstractions.Messaging;
using TestAttarClone.Application.DTOs;
using TestAttarClone.Application.Interfaces;
using TestAttarClone.SharedKernel.Authorization;
using TestAttarClone.SharedKernel.BaseTypes;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace TestAttarClone.Application.Queries.Identity.GetPermissions;

public sealed record GetPermissionsQuery(
    int PageNumber = 1,
    int PageSize = 20,
    string? SearchTerm = null,
    string? GroupKey = null,
    bool IncludeCore = true,
    bool IncludeCustom = true) : IQuery<PermissionListResultDto>;

public sealed class GetPermissionsQueryHandler : IQueryHandler<GetPermissionsQuery, PermissionListResultDto>
{
    private readonly IAccessPermissionRepository _permissionRepository;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IPermissionDefinitionService _permissionDefinitionService;

    public GetPermissionsQueryHandler(
        IAccessPermissionRepository permissionRepository,
        RoleManager<IdentityRole> roleManager,
        IPermissionDefinitionService permissionDefinitionService)
    {
        _permissionRepository = permissionRepository;
        _roleManager = roleManager;
        _permissionDefinitionService = permissionDefinitionService;
    }

    public async Task<Result<PermissionListResultDto>> Handle(GetPermissionsQuery request, CancellationToken cancellationToken)
    {
        var lookup = await _permissionDefinitionService.GetDefinitionsLookupAsync(cancellationToken);

        var rolesByPermission = await BuildRoleLookupAsync(lookup.Keys, cancellationToken);

        var customPermissions = await _permissionRepository.GetCustomAsync(cancellationToken);
        var storedKeys = new HashSet<string>(
            customPermissions.Select(permission => permission.Key),
            StringComparer.OrdinalIgnoreCase);

        var groupDisplayLookup = PermissionCatalog.Groups
            .ToDictionary(
                group => group.Key,
                group => group.DisplayName,
                StringComparer.OrdinalIgnoreCase);

        var customGroupLookup = customPermissions
            .GroupBy(permission => PermissionGroupUtility.NormalizeGroupKey(permission.GroupKey), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group =>
                {
                    var displayName = group.First().GroupDisplayName;
                    if (!string.IsNullOrWhiteSpace(displayName))
                    {
                        return displayName;
                    }

                    return PermissionGroupUtility.ResolveGroupDisplayName(group.Key, group.First().GroupKey);
                },
                StringComparer.OrdinalIgnoreCase);

        foreach (var (key, value) in customGroupLookup)
        {
            groupDisplayLookup[key] = value;
        }

        var permissionToGroupLookup = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var group in PermissionCatalog.Groups)
        {
            foreach (var permission in group.Permissions)
            {
                permissionToGroupLookup[permission.Key] = group.Key;
            }
        }

        foreach (var permission in customPermissions)
        {
            var normalizedGroup = PermissionGroupUtility.NormalizeGroupKey(permission.GroupKey);
            permissionToGroupLookup[permission.Key] = normalizedGroup;
        }

        static string ResolveGroupKey(
            string permissionKey,
            IReadOnlyDictionary<string, string> permissionLookup,
            string? storedGroupKey = null)
        {
            if (!string.IsNullOrWhiteSpace(storedGroupKey))
            {
                return PermissionGroupUtility.NormalizeGroupKey(storedGroupKey);
            }

            if (permissionLookup.TryGetValue(permissionKey, out var groupKey))
            {
                return groupKey;
            }

            var segments = permissionKey.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            return segments.Length > 0 ? segments[0] : "custom";
        }

        static string ResolveGroupDisplayName(
            string groupKey,
            IReadOnlyDictionary<string, string> groupDisplayLookup,
            string permissionKey,
            string? fallbackLabel = null)
        {
            if (groupDisplayLookup.TryGetValue(groupKey, out var displayName))
            {
                return displayName;
            }

            var resolved = PermissionGroupUtility.ResolveGroupDisplayName(groupKey, fallbackLabel);
            if (!string.IsNullOrWhiteSpace(resolved) && !string.Equals(resolved, groupKey, StringComparison.OrdinalIgnoreCase))
            {
                return resolved;
            }

            var segments = permissionKey.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (segments.Length > 0 && groupDisplayLookup.TryGetValue(segments[0], out var derivedDisplay))
            {
                return derivedDisplay;
            }

            if (!string.IsNullOrWhiteSpace(resolved))
            {
                return resolved;
            }

            return "مجوزهای سفارشی";
        }

        var staticItems = lookup.Values
            .Where(definition => !definition.IsCustom && !storedKeys.Contains(definition.Key))
            .Select(definition =>
            {
                var groupKey = ResolveGroupKey(definition.Key, permissionToGroupLookup);
                var groupDisplay = ResolveGroupDisplayName(groupKey, groupDisplayLookup, definition.Key);

                return new PermissionListItemDto(
                    Id: null,
                    definition.Key,
                    definition.DisplayName,
                    definition.Description,
                    definition.IsCore,
                    definition.IsCustom,
                    CreatedAt: null,
                    AssignedRoles: rolesByPermission.TryGetValue(definition.Key, out var roles)
                        ? roles
                        : Array.Empty<string>(),
                    groupKey,
                    groupDisplay);
            })
            .ToList();

        var customItems = customPermissions
            .Select(permission =>
            {
                var groupKey = ResolveGroupKey(permission.Key, permissionToGroupLookup, permission.GroupKey);
                var groupDisplay = ResolveGroupDisplayName(groupKey, groupDisplayLookup, permission.Key, permission.GroupDisplayName);

                return new PermissionListItemDto(
                    permission.Id,
                    permission.Key,
                    permission.DisplayName,
                    permission.Description,
                    permission.IsCore,
                    IsCustom: !permission.IsCore,
                    permission.CreatedAt,
                    AssignedRoles: rolesByPermission.TryGetValue(permission.Key, out var roles)
                        ? roles
                        : Array.Empty<string>(),
                    groupKey,
                    groupDisplay);
            })
            .ToList();

        var allItems = staticItems
            .Concat(customItems)
            .ToList();

        var overallCount = allItems.Count;
        var overallCustomCount = allItems.Count(item => item.IsCustom);
        var overallCoreCount = allItems.Count(item => item.IsCore);

        var searchTerm = request.SearchTerm?.Trim();
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            searchTerm = null;
        }
        IEnumerable<PermissionListItemDto> filteredItems = allItems;

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            filteredItems = filteredItems.Where(item =>
                item.DisplayName.Contains(searchTerm, StringComparison.CurrentCultureIgnoreCase) ||
                (!string.IsNullOrWhiteSpace(item.Description) &&
                    item.Description.Contains(searchTerm, StringComparison.CurrentCultureIgnoreCase)) ||
                item.Key.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(request.GroupKey))
        {
            filteredItems = filteredItems.Where(item =>
                string.Equals(item.GroupKey, request.GroupKey, StringComparison.OrdinalIgnoreCase));
        }

        if (!request.IncludeCore)
        {
            filteredItems = filteredItems.Where(item => !item.IsCore);
        }

        if (!request.IncludeCustom)
        {
            filteredItems = filteredItems.Where(item => !item.IsCustom);
        }

        var filteredList = filteredItems
            .OrderByDescending(item => item.IsCore)
            .ThenBy(item => item.GroupDisplayName, StringComparer.CurrentCulture)
            .ThenBy(item => item.DisplayName, StringComparer.CurrentCulture)
            .ToList();

        var filteredCount = filteredList.Count;
        var filteredCustomCount = filteredList.Count(item => item.IsCustom);
        var filteredCoreCount = filteredList.Count(item => item.IsCore);

        var pageSize = request.PageSize <= 0 ? 20 : Math.Min(request.PageSize, 100);
        var totalPages = filteredCount == 0 ? 1 : (int)Math.Ceiling(filteredCount / (double)pageSize);
        var pageNumber = request.PageNumber <= 0 ? 1 : request.PageNumber;
        pageNumber = Math.Min(pageNumber, totalPages);

        var skip = (pageNumber - 1) * pageSize;
        var pagedItems = filteredList.Skip(skip).Take(pageSize).ToList();

        var groupedItems = pagedItems
            .GroupBy(item => new { item.GroupKey, item.GroupDisplayName })
            .OrderByDescending(group => group.Any(item => item.IsCore))
            .ThenBy(group => group.Key.GroupDisplayName, StringComparer.CurrentCulture)
            .Select(group => new PermissionListGroupDto(
                group.Key.GroupKey,
                group.Key.GroupDisplayName,
                group.ToArray()))
            .ToArray();

        var groupFilter = string.IsNullOrWhiteSpace(request.GroupKey) ? null : request.GroupKey;

        var result = new PermissionListResultDto(
            groupedItems,
            pageNumber,
            pageSize,
            filteredCount,
            overallCount,
            overallCustomCount,
            overallCoreCount,
            filteredCustomCount,
            filteredCoreCount,
            searchTerm,
            groupFilter,
            request.IncludeCore,
            request.IncludeCustom);

        return Result<PermissionListResultDto>.Success(result);
    }

    private async Task<Dictionary<string, IReadOnlyCollection<string>>> BuildRoleLookupAsync(IEnumerable<string> permissionKeys, CancellationToken cancellationToken)
    {
        var keys = new HashSet<string>(permissionKeys, StringComparer.OrdinalIgnoreCase);
        var lookup = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

        var roles = await _roleManager.Roles
            .OrderBy(role => role.Name)
            .ToListAsync(cancellationToken);

        foreach (var role in roles)
        {
            if (string.IsNullOrWhiteSpace(role.Name))
            {
                continue;
            }

            var claims = await _roleManager.GetClaimsAsync(role);
            foreach (var claim in claims)
            {
                if (!string.Equals(claim.Type, PermissionCatalog.ClaimType, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var value = claim.Value;
                if (string.IsNullOrWhiteSpace(value) || !keys.Contains(value))
                {
                    continue;
                }

                if (!lookup.TryGetValue(value, out var roleSet))
                {
                    roleSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    lookup[value] = roleSet;
                }

                roleSet.Add(role.Name);
            }
        }

        return lookup.ToDictionary(
            pair => pair.Key,
            pair => (IReadOnlyCollection<string>)pair.Value
                .OrderBy(name => name, StringComparer.CurrentCulture)
                .ToArray(),
            StringComparer.OrdinalIgnoreCase);
    }
}

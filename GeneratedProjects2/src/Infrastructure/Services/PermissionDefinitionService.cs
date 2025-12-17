using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LogsDtoCloneTest.Application.DTOs;
using LogsDtoCloneTest.Application.Interfaces;
using LogsDtoCloneTest.Infrastructure.Persistence;
using LogsDtoCloneTest.SharedKernel.Authorization;
using Microsoft.EntityFrameworkCore;

namespace LogsDtoCloneTest.Infrastructure.Services;

public sealed class PermissionDefinitionService : IPermissionDefinitionService
{
    private readonly AppDbContext _dbContext;

    public PermissionDefinitionService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<PermissionGroupDto>> GetPermissionGroupsAsync(CancellationToken cancellationToken)
    {
        var groups = new List<PermissionGroupDto>();

        var staticGroups = PermissionCatalog.Groups
            .Select(group => new PermissionGroupDto(
                group.Key,
                group.DisplayName,
                group.Permissions
                    .Select(permission => new PermissionDefinitionDto(
                        permission.Key,
                        permission.DisplayName,
                        permission.Description,
                        IsCustom: false,
                        IsCore: true))
                    .ToArray()))
            .ToList();

        groups.AddRange(staticGroups);

        var customPermissions = await _dbContext.AccessPermissions
            .AsNoTracking()
            .OrderBy(permission => permission.GroupKey)
            .ThenBy(permission => permission.DisplayName)
            .ToListAsync(cancellationToken);

        var groupedCustom = customPermissions
            .GroupBy(permission => PermissionGroupUtility.NormalizeGroupKey(permission.GroupKey), StringComparer.OrdinalIgnoreCase);

        foreach (var customGroup in groupedCustom)
        {
            var normalizedKey = customGroup.Key;
            var storedDisplayName = customGroup.First().GroupDisplayName;
            var displayName = string.IsNullOrWhiteSpace(storedDisplayName)
                ? PermissionGroupUtility.ResolveGroupDisplayName(normalizedKey, customGroup.First().GroupKey)
                : storedDisplayName;

            var definitions = customGroup
                .Select(permission => new PermissionDefinitionDto(
                    permission.Key,
                    permission.DisplayName,
                    permission.Description,
                    IsCustom: !permission.IsCore,
                    permission.IsCore))
                .ToArray();

            var existingIndex = groups.FindIndex(group =>
                string.Equals(group.Key, normalizedKey, StringComparison.OrdinalIgnoreCase));

            if (existingIndex >= 0)
            {
                var merged = groups[existingIndex].Permissions
                    .Concat(definitions)
                    .OrderBy(permission => permission.DisplayName, StringComparer.CurrentCulture)
                    .ToArray();

                groups[existingIndex] = new PermissionGroupDto(
                    groups[existingIndex].Key,
                    groups[existingIndex].DisplayName,
                    merged);
            }
            else
            {
                groups.Add(new PermissionGroupDto(normalizedKey, displayName, definitions));
            }
        }

        return groups;
    }

    public async Task<IReadOnlyDictionary<string, PermissionDefinitionDto>> GetDefinitionsLookupAsync(CancellationToken cancellationToken)
    {
        var lookup = new Dictionary<string, PermissionDefinitionDto>(StringComparer.OrdinalIgnoreCase);

        foreach (var permission in PermissionCatalog.Permissions.Values)
        {
            lookup[permission.Key] = new PermissionDefinitionDto(
                permission.Key,
                permission.DisplayName,
                permission.Description,
                IsCustom: false,
                IsCore: true);
        }

        var customPermissions = await _dbContext.AccessPermissions
            .AsNoTracking()
            .Select(permission => new PermissionDefinitionDto(
                permission.Key,
                permission.DisplayName,
                permission.Description,
                !permission.IsCore,
                permission.IsCore))
            .ToListAsync(cancellationToken);

        foreach (var permission in customPermissions)
        {
            lookup[permission.Key] = permission;
        }

        return lookup;
    }

    public async Task<HashSet<string>> GetAllKeysAsync(CancellationToken cancellationToken)
    {
        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var customKeys = await _dbContext.AccessPermissions
            .AsNoTracking()
            .Select(permission => permission.Key)
            .ToListAsync(cancellationToken);

        foreach (var key in customKeys)
        {
            keys.Add(key);
        }

        return keys;
    }
}

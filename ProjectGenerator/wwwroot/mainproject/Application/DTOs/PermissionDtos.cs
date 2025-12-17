using System;
using System.Collections.Generic;

namespace Attar.Application.DTOs;

public sealed record PermissionListItemDto(
    Guid? Id,
    string Key,
    string DisplayName,
    string? Description,
    bool IsCore,
    bool IsCustom,
    DateTimeOffset? CreatedAt,
    IReadOnlyCollection<string> AssignedRoles,
    string GroupKey,
    string GroupDisplayName);

public sealed record PermissionDetailsDto(
    Guid Id,
    string Key,
    string DisplayName,
    string? Description,
    bool IsCore,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    string GroupKey,
    string GroupDisplayName);

public sealed record PermissionCatalogDto(
    IReadOnlyCollection<PermissionGroupDto> Groups,
    IReadOnlyDictionary<string, PermissionDefinitionDto> Lookup);

public sealed record PermissionListGroupDto(
    string Key,
    string DisplayName,
    IReadOnlyCollection<PermissionListItemDto> Permissions);

public sealed record PermissionListResultDto(
    IReadOnlyCollection<PermissionListGroupDto> Groups,
    int PageNumber,
    int PageSize,
    int FilteredCount,
    int OverallCount,
    int OverallCustomCount,
    int OverallCoreCount,
    int FilteredCustomCount,
    int FilteredCoreCount,
    string? SearchTerm,
    string? GroupKey,
    bool IncludeCore,
    bool IncludeCustom);

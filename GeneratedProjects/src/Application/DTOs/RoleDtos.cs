namespace TestAttarClone.Application.DTOs;

public sealed record RoleSummaryDto(string Id, string Name, string DisplayName);

public sealed record RoleAccessLevelDto(
    string Id,
    string Name,
    string DisplayName,
    IReadOnlyCollection<string> Permissions,
    int UserCount);

public sealed record PermissionGroupDto(
    string Key,
    string DisplayName,
    IReadOnlyCollection<PermissionDefinitionDto> Permissions);

public sealed record PermissionDefinitionDto(
    string Key,
    string DisplayName,
    string? Description,
    bool IsCustom,
    bool IsCore);

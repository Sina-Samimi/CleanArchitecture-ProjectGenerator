namespace LogsDtoCloneTest.Application.DTOs;

public sealed record RegisterUserDto(
    string UserName,
    string? Password,
    string FullName,
    string PhoneNumber,
    IReadOnlyCollection<string>? Roles = null,
    bool IsActive = true,
    string? DeactivationReason = null,
    string? AvatarPath = null);

public sealed record UpdateUserDto(
    string UserId,
    string? Email,
    string? FullName,
    IReadOnlyCollection<string>? Roles,
    bool? IsActive = null,
    string? AvatarPath = null,
    string? PhoneNumber = null,
    string? Password = null);

public sealed record RoleMembershipDto(
    string Name,
    string DisplayName);

public sealed record UserDto(
    string Id,
    string Email,
    string FullName,
    bool IsActive,
    bool IsDeleted,
    DateTimeOffset? DeactivatedOn,
    DateTimeOffset? DeletedOn,
    string PhoneNumber,
    string? AvatarPath,
    IReadOnlyCollection<RoleMembershipDto> Roles,
    DateTimeOffset LastModifiedOn,
    bool IsOnline = false,
    DateTimeOffset? LastSeenAt = null);

public sealed record UserLookupDto(
    string Id,
    string DisplayName,
    string? Email,
    bool IsActive,
    string? PhoneNumber = null);

using System;
using System.Collections.Generic;
using Arsis.Application.Queries.Identity.GetUsers;

namespace EndPoint.WebSite.Areas.Admin.Models;

public sealed record UserListItemViewModel(
    string Id,
    string Email,
    string FullName,
    bool IsActive,
    bool IsDeleted,
    DateTimeOffset? DeactivatedOn,
    DateTimeOffset? DeletedOn,
    DateTimeOffset LastModifiedOn,
    string PhoneNumber,
    string? AvatarPath,
    IReadOnlyCollection<string> Roles,
    IReadOnlyCollection<string> RoleDisplayNames);

public sealed class UserListViewModel
{
    public bool IncludeDeactivated { get; init; }

    public bool IncludeDeleted { get; init; }

    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = 10;

    public int TotalCount { get; init; }

    public string? FullName { get; init; }

    public string? PhoneNumber { get; init; }

    public string? Role { get; init; }

    public UserStatusFilter Status { get; init; } = UserStatusFilter.All;

    public string? RegisteredFrom { get; init; }

    public string? RegisteredTo { get; init; }

    public IReadOnlyCollection<RoleOptionViewModel> AvailableRoles { get; init; } = Array.Empty<RoleOptionViewModel>();

    public IReadOnlyCollection<UserListItemViewModel> Users { get; init; } = Array.Empty<UserListItemViewModel>();

    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}

public sealed class UserListFilterInput
{
    public bool IncludeDeactivated { get; set; } = true;

    public bool IncludeDeleted { get; set; } = false;

    public string? FullName { get; set; }

    public string? PhoneNumber { get; set; }

    public string? Role { get; set; }

    public UserStatusFilter Status { get; set; } = UserStatusFilter.All;

    public string? RegisteredFrom { get; set; }

    public string? RegisteredTo { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 10;
}

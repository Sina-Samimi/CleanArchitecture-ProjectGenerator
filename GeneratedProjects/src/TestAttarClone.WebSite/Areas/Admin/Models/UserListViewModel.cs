using System;
using System.Collections.Generic;
using TestAttarClone.Application.DTOs;
using TestAttarClone.Application.Queries.Identity.GetUsers;

namespace TestAttarClone.WebSite.Areas.Admin.Models;

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
    IReadOnlyCollection<string> RoleDisplayNames,
    bool IsOnline = false,
    DateTimeOffset? LastSeenAt = null);

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

public sealed class UserProfileViewModel
{
    public string Id { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string PhoneNumber { get; init; } = string.Empty;
    public string? AvatarPath { get; init; }
    public bool IsActive { get; init; }
    public bool IsDeleted { get; init; }
    public bool IsOnline { get; init; }
    public DateTimeOffset CreatedOn { get; init; }
    public DateTimeOffset LastModifiedOn { get; init; }
    public DateTimeOffset? DeactivatedOn { get; init; }
    public DateTimeOffset? DeletedOn { get; init; }
    public string? DeactivationReason { get; init; }
    public DateTimeOffset? LastSeenAt { get; init; }
    public IReadOnlyCollection<RoleMembershipDto> Roles { get; init; } = Array.Empty<RoleMembershipDto>();
    public int TotalInvoices { get; init; }
    public int TotalOrders { get; init; }
    public decimal TotalSpent { get; init; }
}
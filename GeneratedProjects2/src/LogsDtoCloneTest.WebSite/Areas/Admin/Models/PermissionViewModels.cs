using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LogsDtoCloneTest.WebSite.Areas.Admin.Models;

public sealed record PermissionListItemViewModel(
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

public sealed class PermissionListViewModel
{
    public IReadOnlyCollection<PermissionListGroupViewModel> Groups { get; init; } = Array.Empty<PermissionListGroupViewModel>();

    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    public int TotalCount { get; init; }

    public int TotalPermissions { get; init; }

    public int TotalCustomPermissions { get; init; }

    public int TotalCorePermissions { get; init; }

    public int FilteredCustomPermissions { get; init; }

    public int FilteredCorePermissions { get; init; }

    public string? SearchTerm { get; init; }

    public string? GroupKey { get; init; }

    public bool IncludeCore { get; init; } = true;

    public bool IncludeCustom { get; init; } = true;

    public IReadOnlyCollection<PermissionGroupOptionViewModel> AvailableGroups { get; init; }
        = Array.Empty<PermissionGroupOptionViewModel>();

    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}

public sealed record PermissionListGroupViewModel(
    string Key,
    string DisplayName,
    IReadOnlyCollection<PermissionListItemViewModel> Permissions);

public sealed record PermissionGroupOptionViewModel(string Key, string DisplayName, bool IsCustom);

public sealed class EditPermissionViewModel
{
    public Guid? Id { get; set; }

    public string? Key { get; set; } = string.Empty;

    [Display(Name = "گروه مجوز")]
    public string? GroupKey { get; set; } = string.Empty;

    [Display(Name = "عنوان زیرمجموعه گروه")]
    [MaxLength(128, ErrorMessage = "عنوان گروه نباید بیش از ۱۲۸ کاراکتر باشد.")]
    public string? GroupDisplayName { get; set; } = string.Empty;

    public IReadOnlyCollection<PermissionGroupOptionViewModel> GroupOptions { get; set; }
        = Array.Empty<PermissionGroupOptionViewModel>();

    [Required(ErrorMessage = "نام دسترسی را وارد کنید.")]
    [Display(Name = "نام دسترسی")]
    [MaxLength(256, ErrorMessage = "نام دسترسی نباید بیش از ۲۵۶ کاراکتر باشد.")]
    public string DisplayName { get; set; } = string.Empty;

    [Display(Name = "توضیحات")]
    [MaxLength(1024, ErrorMessage = "توضیح نباید بیش از ۱۰۲۴ کاراکتر باشد.")]
    public string? Description { get; set; }

    [Display(Name = "تعیین به عنوان Core")]
    public bool IsCore { get; set; }
}

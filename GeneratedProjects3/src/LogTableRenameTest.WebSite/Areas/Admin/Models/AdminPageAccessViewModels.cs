using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LogTableRenameTest.WebSite.Areas.Admin.Models;

public sealed record PageAccessPermissionItemViewModel(
    string Key,
    string DisplayName,
    bool IsCore,
    bool IsCustom);

public sealed record PageAccessPageViewModel(
    string Area,
    string Controller,
    string Action,
    string DisplayName,
    IReadOnlyCollection<PageAccessPermissionItemViewModel> Permissions);

public sealed record PageAccessPermissionOptionViewModel(
    string Key,
    string DisplayName,
    string? Description,
    bool IsCore,
    bool IsCustom);

public sealed record PageAccessAreaOptionViewModel(
    string Value,
    string DisplayName);

public enum PageAccessRestrictionFilter
{
    All = 0,
    Restricted = 1,
    Unrestricted = 2
}

public sealed class PageAccessIndexFilterViewModel
{
    public string Search { get; init; } = string.Empty;

    public string Area { get; init; } = string.Empty;

    public string Permission { get; init; } = string.Empty;

    public PageAccessRestrictionFilter Restriction { get; init; } = PageAccessRestrictionFilter.All;
}

public sealed class PageAccessIndexRequest
{
    public string? Search { get; set; }
        = string.Empty;

    public string? Area { get; set; }
        = string.Empty;

    public string? Permission { get; set; }
        = string.Empty;

    public PageAccessRestrictionFilter Restriction { get; set; }
        = PageAccessRestrictionFilter.All;

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 10;
}

public sealed class PageAccessIndexViewModel
{
    public IReadOnlyCollection<PageAccessPageViewModel> Pages { get; init; }
        = Array.Empty<PageAccessPageViewModel>();

    public IReadOnlyCollection<PageAccessPermissionOptionViewModel> PermissionOptions { get; init; }
        = Array.Empty<PageAccessPermissionOptionViewModel>();

    public IReadOnlyCollection<PageAccessAreaOptionViewModel> AreaOptions { get; init; }
        = Array.Empty<PageAccessAreaOptionViewModel>();

    public PageAccessIndexFilterViewModel Filters { get; init; } = new();

    public int TotalCount { get; init; }
        = 0;

    public int FilteredCount { get; init; }
        = 0;

    public int PageNumber { get; init; }
        = 1;

    public int PageSize { get; init; }
        = 10;

    public int TotalPages { get; init; }
        = 1;

    public int FirstItemIndex { get; init; }
        = 0;

    public int LastItemIndex { get; init; }
        = 0;
}

public sealed class EditPageAccessViewModel
{
    public string Area { get; set; } = string.Empty;

    public string Controller { get; set; } = string.Empty;

    public string Action { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public List<string> SelectedPermissions { get; set; } = new();

    public IReadOnlyCollection<PageAccessPermissionOptionViewModel> AvailablePermissions { get; set; }
        = Array.Empty<PageAccessPermissionOptionViewModel>();
}

public sealed class SavePageAccessInputModel
{
    public string Area { get; set; } = string.Empty;

    [Required(ErrorMessage = "شناسه کنترلر نامعتبر است.")]
    public string Controller { get; set; } = string.Empty;

    [Required(ErrorMessage = "شناسه اکشن نامعتبر است.")]
    public string Action { get; set; } = string.Empty;

    public List<string> Permissions { get; set; } = new();
}

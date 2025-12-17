using System.ComponentModel.DataAnnotations;

namespace LogsDtoCloneTest.WebSite.Areas.Admin.Models;

public sealed record AccessLevelCardPermissionViewModel(string Key, string DisplayName);

public sealed record AccessLevelCardViewModel(
    string Id,
    string Name,
    string DisplayName,
    int UserCount,
    IReadOnlyCollection<AccessLevelCardPermissionViewModel> Permissions);

public sealed record PermissionDefinitionViewModel(string Key, string DisplayName, string? Description, bool IsCustom, bool IsCore);

public sealed record PermissionGroupDefinitionViewModel(
    string Key,
    string DisplayName,
    IReadOnlyCollection<PermissionDefinitionViewModel> Permissions);

public sealed record PermissionSelectionViewModel(
    string Key,
    string DisplayName,
    string? Description,
    bool IsSelected,
    bool IsCustom,
    bool IsCore);

public sealed record PermissionSelectionGroupViewModel(
    string Key,
    string DisplayName,
    IReadOnlyCollection<PermissionSelectionViewModel> Permissions);

public sealed class AccessLevelListViewModel
{
    public IReadOnlyCollection<AccessLevelCardViewModel> Roles { get; init; } = Array.Empty<AccessLevelCardViewModel>();

    public IReadOnlyCollection<PermissionGroupDefinitionViewModel> PermissionGroups { get; init; } = Array.Empty<PermissionGroupDefinitionViewModel>();
}

public sealed class EditAccessLevelViewModel
{
    public string? Id { get; set; }

    [Required(ErrorMessage = "نام سطح دسترسی را وارد کنید.")]
    [Display(Name = "نام سطح دسترسی")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "عنوان فارسی نقش را وارد کنید.")]
    [Display(Name = "عنوان فارسی نقش")]
    public string DisplayName { get; set; } = string.Empty;

    [Display(Name = "دسترسی‌های انتخاب‌شده")]
    public List<string> SelectedPermissions { get; set; } = new();

    public IReadOnlyCollection<PermissionSelectionGroupViewModel> PermissionGroups { get; set; } = Array.Empty<PermissionSelectionGroupViewModel>();
}

public sealed record RoleOptionViewModel(string Value, string Label);

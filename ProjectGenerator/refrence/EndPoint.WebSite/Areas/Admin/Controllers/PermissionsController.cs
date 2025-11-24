using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arsis.Application.Commands.Identity.Permissions;
using Arsis.Application.Queries.Identity.GetPermissions;
using Arsis.SharedKernel.Authorization;
using EndPoint.WebSite.Areas.Admin.Models;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EndPoint.WebSite.Areas.Admin.Controllers;

[Area("Admin")]
public sealed class PermissionsController : Controller
{
    private readonly IMediator _mediator;

    public PermissionsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        int page = 1,
        int pageSize = 20,
        string? search = null,
        string? group = null,
        bool includeCore = true,
        bool includeCustom = true)
    {
        var trimmedGroup = string.IsNullOrWhiteSpace(group) ? null : group.Trim();
        var result = await _mediator.Send(new GetPermissionsQuery(page, pageSize, search, trimmedGroup, includeCore, includeCustom));
        var groupOptions = BuildGroupOptions();
        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error;
            return View(new PermissionListViewModel
            {
                PageNumber = page,
                PageSize = pageSize,
                SearchTerm = search,
                GroupKey = trimmedGroup,
                IncludeCore = includeCore,
                IncludeCustom = includeCustom,
                AvailableGroups = groupOptions
            });
        }

        var dto = result.Value!;
        groupOptions = BuildGroupOptions(dto.Groups.Select(groupDto => (groupDto.Key, groupDto.DisplayName)));

        var groups = dto.Groups
            .Select(group => new PermissionListGroupViewModel(
                group.Key,
                group.DisplayName,
                group.Permissions
                    .Select(permission => new PermissionListItemViewModel(
                        permission.Id,
                        permission.Key,
                        permission.DisplayName,
                        permission.Description,
                        permission.IsCore,
                        permission.IsCustom,
                        permission.CreatedAt,
                        permission.AssignedRoles,
                        permission.GroupKey,
                        permission.GroupDisplayName))
                    .ToArray()))
            .ToArray();

        var viewModel = new PermissionListViewModel
        {
            Groups = groups,
            PageNumber = dto.PageNumber,
            PageSize = dto.PageSize,
            TotalCount = dto.FilteredCount,
            TotalPermissions = dto.OverallCount,
            TotalCustomPermissions = dto.OverallCustomCount,
            TotalCorePermissions = dto.OverallCoreCount,
            FilteredCustomPermissions = dto.FilteredCustomCount,
            FilteredCorePermissions = dto.FilteredCoreCount,
            SearchTerm = dto.SearchTerm,
            GroupKey = dto.GroupKey,
            IncludeCore = dto.IncludeCore,
            IncludeCustom = dto.IncludeCustom,
            AvailableGroups = groupOptions
        };

        return View(viewModel);
    }

    private static IReadOnlyCollection<PermissionGroupOptionViewModel> BuildGroupOptions(
        IEnumerable<(string Key, string DisplayName)>? additionalGroups = null)
    {
        var comparer = StringComparer.OrdinalIgnoreCase;
        var catalogKeys = new HashSet<string>(
            PermissionCatalog.Groups.Select(group => group.Key),
            comparer);
        var seen = new HashSet<string>(comparer);
        var options = new List<PermissionGroupOptionViewModel>();

        foreach (var group in PermissionCatalog.Groups.OrderBy(group => group.DisplayName, StringComparer.CurrentCulture))
        {
            if (seen.Add(group.Key))
            {
                options.Add(new PermissionGroupOptionViewModel(group.Key, group.DisplayName, false));
            }
        }

        if (additionalGroups is not null)
        {
            foreach (var (Key, DisplayName) in additionalGroups)
            {
                if (string.IsNullOrWhiteSpace(Key))
                {
                    continue;
                }

                var normalizedKey = PermissionGroupUtility.NormalizeGroupKey(Key);
                if (!seen.Add(normalizedKey))
                {
                    continue;
                }

                var resolvedDisplayName = PermissionGroupUtility.ResolveGroupDisplayName(normalizedKey, DisplayName);
                var isCustom = !catalogKeys.Contains(normalizedKey)
                    || string.Equals(normalizedKey, "custom", StringComparison.OrdinalIgnoreCase);

                options.Add(new PermissionGroupOptionViewModel(normalizedKey, resolvedDisplayName, isCustom));
            }
        }

        if (!seen.Contains("custom"))
        {
            options.Add(new PermissionGroupOptionViewModel("custom", "مجوزهای سفارشی", true));
            seen.Add("custom");
        }

        return options
            .OrderBy(option => option.DisplayName, StringComparer.CurrentCulture)
            .ToArray();
    }

    [HttpGet]
    public async Task<IActionResult> Create(string? group = null)
    {
        var model = new EditPermissionViewModel
        {
            GroupKey = group ?? string.Empty,
            GroupDisplayName = group
        };

        ApplyGroupMetadata(model, group);
        await PopulateGroupOptionsAsync(model);
        return PartialView("_PermissionModal", model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var result = await _mediator.Send(new GetPermissionByIdQuery(id));
        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error;
            return RedirectToAction(nameof(Index));
        }

        var dto = result.Value!;
        var model = new EditPermissionViewModel
        {
            Id = dto.Id,
            Key = dto.Key,
            DisplayName = dto.DisplayName,
            Description = dto.Description,
            IsCore = dto.IsCore,
            GroupKey = dto.GroupKey,
            GroupDisplayName = dto.GroupDisplayName
        };

        ApplyGroupMetadata(model);
        await PopulateGroupOptionsAsync(model);

        return PartialView("_PermissionModal", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(EditPermissionViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ApplyGroupMetadata(model);
            await PopulateGroupOptionsAsync(model);
            return PartialView("_PermissionModal", model);
        }

        var payload = new SavePermissionPayload(
            model.Id,
            model.DisplayName,
            model.Description,
            model.IsCore,
            model.GroupKey,
            model.GroupDisplayName);

        var result = await _mediator.Send(new SavePermissionCommand(payload));
        if (!result.IsSuccess)
        {
            ApplyCommandError(result.Error);
            ApplyGroupMetadata(model);
            await PopulateGroupOptionsAsync(model);
            return PartialView("_PermissionModal", model);
        }

        TempData["Success"] = model.Id.HasValue
            ? "مجوز با موفقیت به‌روزرسانی شد."
            : "مجوز جدید با موفقیت ایجاد شد.";

        return Json(new { success = true, redirectUrl = Url.Action(nameof(Index)) });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _mediator.Send(new DeletePermissionCommand(id));
        if (!result.IsSuccess)
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            return Json(new { success = false, error = result.Error });
        }

        TempData["Success"] = "مجوز با موفقیت حذف شد.";
        return Json(new { success = true, redirectUrl = Url.Action(nameof(Index)) });
    }

    private static void ApplyGroupMetadata(EditPermissionViewModel model, string? fallbackGroupLabel = null)
    {
        if (string.IsNullOrWhiteSpace(model.GroupKey))
        {
            model.GroupKey = string.Empty;
            model.GroupDisplayName = string.Empty;
            return;
        }

        var normalizedKey = PermissionGroupUtility.NormalizeGroupKey(model.GroupKey);
        model.GroupKey = normalizedKey;

        var resolvedLabel = model.GroupDisplayName;
        if (string.IsNullOrWhiteSpace(resolvedLabel))
        {
            resolvedLabel = fallbackGroupLabel;
        }

        model.GroupDisplayName = PermissionGroupUtility.ResolveGroupDisplayName(
            normalizedKey,
            resolvedLabel?.Trim());
    }

    private async Task PopulateGroupOptionsAsync(EditPermissionViewModel model, CancellationToken cancellationToken = default)
    {
        var aggregatedGroups = new List<(string Key, string DisplayName)>();

        var catalogResult = await _mediator.Send(new GetPermissionCatalogQuery(), cancellationToken);
        if (catalogResult.IsSuccess && catalogResult.Value is { } catalog)
        {
            aggregatedGroups.AddRange(catalog.Groups.Select(group => (group.Key, group.DisplayName)));
        }

        if (!string.IsNullOrWhiteSpace(model.GroupKey))
        {
            var displayName = string.IsNullOrWhiteSpace(model.GroupDisplayName)
                ? model.GroupKey!
                : model.GroupDisplayName!;

            aggregatedGroups.Add((model.GroupKey!, displayName));
        }

        model.GroupOptions = BuildGroupOptions(aggregatedGroups);
    }

    private void ApplyCommandError(string? errorMessage)
    {
        if (string.IsNullOrWhiteSpace(errorMessage))
        {
            return;
        }

        if (errorMessage.Contains("نام مجوز", StringComparison.CurrentCulture))
        {
            ModelState.AddModelError(nameof(EditPermissionViewModel.DisplayName), errorMessage);
            return;
        }

        if (errorMessage.Contains("توضیح", StringComparison.CurrentCulture))
        {
            ModelState.AddModelError(nameof(EditPermissionViewModel.Description), errorMessage);
            return;
        }

        ModelState.AddModelError(string.Empty, errorMessage);
    }
}

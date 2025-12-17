using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Attar.Application.Commands.Identity.SaveRoleAccessLevel;
using Attar.Application.DTOs;
using Attar.Application.Queries.Identity.GetPermissions;
using Attar.Application.Queries.Identity.GetRoles;
using Attar.WebSite.Areas.Admin.Models;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Attar.WebSite.Areas.Admin.Controllers;

[Area("Admin")]
public sealed class AccessLevelsController : Controller
{
    private readonly IMediator _mediator;

    public AccessLevelsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var rolesResult = await _mediator.Send(new GetRoleAccessLevelsQuery());
        if (!rolesResult.IsSuccess && !string.IsNullOrWhiteSpace(rolesResult.Error))
        {
            TempData["Error"] = rolesResult.Error;
        }

        var catalog = await LoadPermissionCatalogAsync(HttpContext.RequestAborted);
        var lookup = catalog.Lookup ?? new Dictionary<string, PermissionDefinitionDto>(StringComparer.OrdinalIgnoreCase);

        var roleDtos = rolesResult.IsSuccess ? rolesResult.Value! : Array.Empty<RoleAccessLevelDto>();
        var roleCards = roleDtos
            .Select(role => new AccessLevelCardViewModel(
                role.Id,
                role.Name,
                role.DisplayName,
                role.UserCount,
                role.Permissions
                    .Select(permissionKey =>
                    {
                        if (lookup.TryGetValue(permissionKey, out var definition))
                        {
                            return new AccessLevelCardPermissionViewModel(permissionKey, definition.DisplayName);
                        }

                        return new AccessLevelCardPermissionViewModel(permissionKey, permissionKey);
                    })
                    .ToArray()))
            .ToArray();

        var permissionGroups = catalog.Groups
            .Select(group => new PermissionGroupDefinitionViewModel(
                group.Key,
                group.DisplayName,
                group.Permissions
                    .Select(permission => new PermissionDefinitionViewModel(
                        permission.Key,
                        permission.DisplayName,
                        permission.Description,
                        permission.IsCustom,
                        permission.IsCore))
                    .ToArray()))
            .ToArray();

        var viewModel = new AccessLevelListViewModel
        {
            Roles = roleCards,
            PermissionGroups = permissionGroups
        };

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var catalog = await LoadPermissionCatalogAsync(HttpContext.RequestAborted);
        var model = BuildEditModel(catalog, null, string.Empty, string.Empty, Array.Empty<string>());
        return PartialView("_AccessLevelModal", model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return RedirectToAction(nameof(Index));
        }

        var result = await _mediator.Send(new GetRoleAccessLevelByIdQuery(id));
        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error;
            return RedirectToAction(nameof(Index));
        }

        var catalog = await LoadPermissionCatalogAsync(HttpContext.RequestAborted);
        var role = result.Value!;
        var model = BuildEditModel(catalog, role.Id, role.Name, role.DisplayName, role.Permissions);
        return PartialView("_AccessLevelModal", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(EditAccessLevelViewModel model)
    {
        var catalog = await LoadPermissionCatalogAsync(HttpContext.RequestAborted);

        model.SelectedPermissions = NormalizePermissions(model.SelectedPermissions, catalog);
        PopulatePermissions(model, catalog);

        if (!ModelState.IsValid)
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            return PartialView("_AccessLevelModal", model);
        }

        var command = new SaveRoleAccessLevelCommand(new SaveRoleAccessLevelDto(
            model.Id,
            model.Name,
            model.DisplayName,
            model.SelectedPermissions));

        var result = await _mediator.Send(command);
        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            Response.StatusCode = StatusCodes.Status400BadRequest;
            return PartialView("_AccessLevelModal", model);
        }

        TempData["Success"] = string.IsNullOrWhiteSpace(model.Id)
            ? "نقش جدید با موفقیت ایجاد شد."
            : "سطح دسترسی با موفقیت به‌روزرسانی شد.";

        return Json(new { success = true, redirectUrl = Url.Action(nameof(Index)) });
    }

    private async Task<PermissionCatalogDto> LoadPermissionCatalogAsync(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetPermissionCatalogQuery(), cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return new PermissionCatalogDto(
                Array.Empty<PermissionGroupDto>(),
                new Dictionary<string, PermissionDefinitionDto>(StringComparer.OrdinalIgnoreCase));
        }

        return result.Value;
    }

    private EditAccessLevelViewModel BuildEditModel(
        PermissionCatalogDto catalog,
        string? id,
        string? name,
        string? displayName,
        IEnumerable<string> selectedPermissions)
    {
        var model = new EditAccessLevelViewModel
        {
            Id = id,
            Name = name ?? string.Empty,
            DisplayName = displayName ?? string.Empty,
            SelectedPermissions = NormalizePermissions(selectedPermissions, catalog)
        };

        PopulatePermissions(model, catalog);
        return model;
    }

    private static void PopulatePermissions(EditAccessLevelViewModel model, PermissionCatalogDto catalog)
    {
        var selected = new HashSet<string>(model.SelectedPermissions, StringComparer.OrdinalIgnoreCase);

        var groups = catalog.Groups
            .Select(group => new PermissionSelectionGroupViewModel(
                group.Key,
                group.DisplayName,
                group.Permissions
                    .Select(permission => new PermissionSelectionViewModel(
                        permission.Key,
                        permission.DisplayName,
                        permission.Description,
                        selected.Contains(permission.Key),
                        permission.IsCustom,
                        permission.IsCore))
                    .ToArray()))
            .ToArray();

        model.PermissionGroups = groups;
    }

    private static List<string> NormalizePermissions(IEnumerable<string>? permissions, PermissionCatalogDto catalog)
    {
        if (permissions is null)
        {
            return new List<string>();
        }

        var validKeys = new HashSet<string>(catalog.Lookup.Keys, StringComparer.OrdinalIgnoreCase);

        return permissions
            .Where(permission => !string.IsNullOrWhiteSpace(permission))
            .Select(permission => permission.Trim())
            .Where(permission => validKeys.Contains(permission))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}

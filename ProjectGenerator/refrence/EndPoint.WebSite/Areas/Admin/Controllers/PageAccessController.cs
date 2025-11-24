using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Arsis.Application.Commands.Admin.PageAccess;
using Arsis.Application.DTOs;
using Arsis.Application.Queries.Admin.PageAccess;
using Arsis.Application.Queries.Identity.GetPermissions;
using EndPoint.WebSite.Areas.Admin.Models;
using EndPoint.WebSite.Services;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EndPoint.WebSite.Areas.Admin.Controllers;

[Area("Admin")]
public sealed class PageAccessController : Controller
{
    private readonly IMediator _mediator;
    private readonly IPageAccessCache _pageAccessCache;

    public PageAccessController(IMediator mediator, IPageAccessCache pageAccessCache)
    {
        _mediator = mediator;
        _pageAccessCache = pageAccessCache;
    }

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] PageAccessIndexRequest? request)
    {
        request ??= new PageAccessIndexRequest();

        var cancellationToken = HttpContext.RequestAborted;

        var overviewResult = await _mediator.Send(new GetAdminPageAccessOverviewQuery(), cancellationToken);
        if (!overviewResult.IsSuccess && !string.IsNullOrWhiteSpace(overviewResult.Error))
        {
            TempData["Error"] = overviewResult.Error;
        }

        var permissionResult = await _mediator.Send(new GetPermissionCatalogQuery(), cancellationToken);
        if (!permissionResult.IsSuccess && !string.IsNullOrWhiteSpace(permissionResult.Error))
        {
            TempData["Error"] = permissionResult.Error;
        }

        var permissionLookup = BuildPermissionLookup(permissionResult.Value);
        var permissionOptions = BuildPermissionOptions(permissionResult.Value);

        var pages = overviewResult.IsSuccess && overviewResult.Value is not null
            ? overviewResult.Value.Pages
            : Array.Empty<PageAccessEntryDto>();

        var allPages = pages
            .OrderBy(page => page.Descriptor.DisplayName, StringComparer.CurrentCulture)
            .ThenBy(page => page.Descriptor.Controller, StringComparer.OrdinalIgnoreCase)
            .ThenBy(page => page.Descriptor.Action, StringComparer.OrdinalIgnoreCase)
            .Select(page => new PageAccessPageViewModel(
                page.Descriptor.Area ?? string.Empty,
                page.Descriptor.Controller,
                page.Descriptor.Action,
                page.Descriptor.DisplayName,
                page.Permissions
                    .Select(permission => BuildPermissionItem(permission, permissionLookup))
                    .OrderBy(item => item.DisplayName, StringComparer.CurrentCulture)
                    .ToArray()))
            .ToList();

        var areaOptions = allPages
            .Select(page => page.Area ?? string.Empty)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(area => area, StringComparer.CurrentCulture)
            .Select(area => new PageAccessAreaOptionViewModel(
                area,
                string.IsNullOrWhiteSpace(area) ? "بخش عمومی" : area))
            .ToArray();

        var filterState = new PageAccessIndexFilterViewModel
        {
            Search = request.Search?.Trim() ?? string.Empty,
            Area = request.Area?.Trim() ?? string.Empty,
            Permission = request.Permission?.Trim() ?? string.Empty,
            Restriction = request.Restriction
        };

        var filteredPages = allPages.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(filterState.Search))
        {
            var searchTerm = filterState.Search.Trim();
            filteredPages = filteredPages.Where(page =>
                page.DisplayName.Contains(searchTerm, StringComparison.CurrentCultureIgnoreCase) ||
                page.Controller.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                page.Action.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                page.Area.Contains(searchTerm, StringComparison.CurrentCultureIgnoreCase) ||
                page.Permissions.Any(permission =>
                    permission.DisplayName.Contains(searchTerm, StringComparison.CurrentCultureIgnoreCase) ||
                    permission.Key.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)));
        }

        if (!string.IsNullOrWhiteSpace(filterState.Area))
        {
            filteredPages = filteredPages.Where(page =>
                string.Equals(page.Area, filterState.Area, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(filterState.Permission))
        {
            filteredPages = filteredPages.Where(page =>
                page.Permissions.Any(permission =>
                    string.Equals(permission.Key, filterState.Permission, StringComparison.OrdinalIgnoreCase)));
        }

        filteredPages = filterState.Restriction switch
        {
            PageAccessRestrictionFilter.Restricted => filteredPages.Where(page => page.Permissions.Count > 0),
            PageAccessRestrictionFilter.Unrestricted => filteredPages.Where(page => page.Permissions.Count == 0),
            _ => filteredPages
        };

        var filteredList = filteredPages.ToList();
        var filteredCount = filteredList.Count;
        var totalCount = allPages.Count;

        var pageSize = request.PageSize <= 0 ? 10 : Math.Clamp(request.PageSize, 5, 50);
        var totalPages = filteredCount == 0 ? 1 : (int)Math.Ceiling(filteredCount / (double)pageSize);
        var pageNumber = request.Page <= 0 ? 1 : request.Page;
        if (pageNumber > totalPages)
        {
            pageNumber = totalPages;
        }

        var skip = filteredCount == 0 ? 0 : (pageNumber - 1) * pageSize;
        var pagedPages = filteredList
            .Skip(skip)
            .Take(pageSize)
            .ToArray();

        var firstItemIndex = filteredCount == 0 ? 0 : skip + 1;
        var lastItemIndex = filteredCount == 0 ? 0 : skip + pagedPages.Length;

        var viewModel = new PageAccessIndexViewModel
        {
            Pages = pagedPages,
            PermissionOptions = permissionOptions,
            AreaOptions = areaOptions,
            Filters = filterState,
            TotalCount = totalCount,
            FilteredCount = filteredCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalPages = totalPages,
            FirstItemIndex = firstItemIndex,
            LastItemIndex = lastItemIndex
        };

        ViewData["Title"] = "دسترسی صفحات پنل مدیریت";
        ViewData["Subtitle"] = "تعیین مجوزهای ورود به هر بخش مدیریتی";

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(string controller, string action, string? area)
    {
        if (string.IsNullOrWhiteSpace(controller) || string.IsNullOrWhiteSpace(action))
        {
            return RedirectToAction(nameof(Index));
        }

        var cancellationToken = HttpContext.RequestAborted;
        var overviewResult = await _mediator.Send(new GetAdminPageAccessOverviewQuery(), cancellationToken);
        if (!overviewResult.IsSuccess || overviewResult.Value is null)
        {
            TempData["Error"] = overviewResult.Error ?? "امکان بارگذاری صفحات فراهم نشد.";
            return RedirectToAction(nameof(Index));
        }

        var permissionResult = await _mediator.Send(new GetPermissionCatalogQuery(), cancellationToken);
        if (!permissionResult.IsSuccess)
        {
            TempData["Error"] = permissionResult.Error ?? "امکان بارگذاری فهرست مجوزها فراهم نشد.";
            return RedirectToAction(nameof(Index));
        }

        var permissionLookup = BuildPermissionLookup(permissionResult.Value);
        var permissionOptions = BuildPermissionOptions(permissionResult.Value);

        var page = overviewResult.Value.Pages
            .FirstOrDefault(entry =>
                string.Equals(entry.Descriptor.Controller, controller, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(entry.Descriptor.Action, action, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(entry.Descriptor.Area ?? string.Empty, area ?? string.Empty, StringComparison.OrdinalIgnoreCase));

        if (page is null)
        {
            TempData["Error"] = "صفحه مورد نظر یافت نشد.";
            return RedirectToAction(nameof(Index));
        }

        var model = new EditPageAccessViewModel
        {
            Area = page.Descriptor.Area ?? string.Empty,
            Controller = page.Descriptor.Controller,
            Action = page.Descriptor.Action,
            DisplayName = string.IsNullOrWhiteSpace(page.Descriptor.DisplayName)
                ? $"{page.Descriptor.Controller}/{page.Descriptor.Action}"
                : page.Descriptor.DisplayName,
            SelectedPermissions = page.Permissions.ToList(),
            AvailablePermissions = permissionOptions
        };

        return PartialView("_EditModal", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(SavePageAccessInputModel input)
    {
        if (input is null)
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            return Json(new { success = false, message = "درخواست نامعتبر بود." });
        }

        var cancellationToken = HttpContext.RequestAborted;
        var permissionResult = await _mediator.Send(new GetPermissionCatalogQuery(), cancellationToken);
        if (!permissionResult.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, permissionResult.Error ?? "امکان بارگذاری فهرست مجوزها فراهم نشد.");
        }

        var permissionOptions = BuildPermissionOptions(permissionResult.Value);
        var normalizedPermissions = NormalizePermissions(input.Permissions, permissionOptions);

        if (!ModelState.IsValid)
        {
            var invalidModel = new EditPageAccessViewModel
            {
                Area = input.Area ?? string.Empty,
                Controller = input.Controller ?? string.Empty,
                Action = input.Action ?? string.Empty,
                DisplayName = $"{input.Controller}/{input.Action}",
                SelectedPermissions = normalizedPermissions,
                AvailablePermissions = permissionOptions
            };

            Response.StatusCode = StatusCodes.Status400BadRequest;
            return PartialView("_EditModal", invalidModel);
        }

        var command = new SavePageAccessPolicyCommand(
            input.Area ?? string.Empty,
            input.Controller,
            input.Action,
            normalizedPermissions);

        var result = await _mediator.Send(command, cancellationToken);
        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "خطا در ذخیره تنظیمات دسترسی.");
            var invalidModel = new EditPageAccessViewModel
            {
                Area = input.Area ?? string.Empty,
                Controller = input.Controller,
                Action = input.Action,
                DisplayName = $"{input.Controller}/{input.Action}",
                SelectedPermissions = normalizedPermissions,
                AvailablePermissions = permissionOptions
            };

            Response.StatusCode = StatusCodes.Status400BadRequest;
            return PartialView("_EditModal", invalidModel);
        }

        _pageAccessCache.Invalidate();

        TempData["Success"] = "تنظیمات دسترسی صفحه با موفقیت ذخیره شد.";
        return Json(new { success = true, redirectUrl = Url.Action(nameof(Index)) });
    }

    private static IReadOnlyDictionary<string, PermissionDefinitionDto> BuildPermissionLookup(PermissionCatalogDto? catalog)
    {
        if (catalog?.Lookup is not null)
        {
            return catalog.Lookup;
        }

        return new Dictionary<string, PermissionDefinitionDto>(StringComparer.OrdinalIgnoreCase);
    }

    private static IReadOnlyCollection<PageAccessPermissionOptionViewModel> BuildPermissionOptions(PermissionCatalogDto? catalog)
    {
        if (catalog is null)
        {
            return Array.Empty<PageAccessPermissionOptionViewModel>();
        }

        var options = new List<PageAccessPermissionOptionViewModel>();

        foreach (var group in catalog.Groups.OrderBy(group => group.DisplayName, StringComparer.CurrentCulture))
        {
            foreach (var permission in group.Permissions.OrderBy(permission => permission.DisplayName, StringComparer.CurrentCulture))
            {
                options.Add(new PageAccessPermissionOptionViewModel(
                    permission.Key,
                    permission.DisplayName,
                    permission.Description,
                    permission.IsCore,
                    permission.IsCustom));
            }
        }

        return options;
    }

    private static PageAccessPermissionItemViewModel BuildPermissionItem(
        string key,
        IReadOnlyDictionary<string, PermissionDefinitionDto> lookup)
    {
        if (lookup.TryGetValue(key, out var definition))
        {
            return new PageAccessPermissionItemViewModel(
                key,
                definition.DisplayName,
                definition.IsCore,
                definition.IsCustom);
        }

        return new PageAccessPermissionItemViewModel(key, key, false, false);
    }

    private static List<string> NormalizePermissions(
        IEnumerable<string>? selected,
        IReadOnlyCollection<PageAccessPermissionOptionViewModel> options)
    {
        if (selected is null)
        {
            return new List<string>();
        }

        var validKeys = new HashSet<string>(options.Select(option => option.Key), StringComparer.OrdinalIgnoreCase);
        return selected
            .Where(key => !string.IsNullOrWhiteSpace(key))
            .Select(key => key.Trim())
            .Where(validKeys.Contains)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}

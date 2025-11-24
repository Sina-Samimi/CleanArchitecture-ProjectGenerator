using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Arsis.Application.Commands.Identity.DeactivateUser;
using Arsis.Application.Commands.Identity.DeleteUser;
using Arsis.Application.Commands.Identity.RegisterUser;
using Arsis.Application.Commands.Identity.UpdateUser;
using Arsis.Application.DTOs;
using Arsis.Application.Interfaces;
using Arsis.Application.Queries.Identity.GetUserById;
using Arsis.Application.Queries.Identity.GetUsers;
using Arsis.Application.Queries.Identity.GetRoles;
using EndPoint.WebSite.Areas.Admin.Models;
using Arsis.SharedKernel.Extensions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;

namespace EndPoint.WebSite.Areas.Admin.Controllers;

[Area("Admin")]
public sealed class UsersController : Controller
{
    private const int MaxAvatarFileSizeKb = 2 * 1024;
    private const int DefaultPageSize = 10;
    private const int MaxPageSize = 50;
    private const string AvatarUploadFolder = "users/profile";

    private static readonly HashSet<string> AllowedAvatarContentTypes = new(
        new[] { "image/png", "image/jpeg", "image/jpg", "image/webp" },
        StringComparer.OrdinalIgnoreCase);

    private readonly IMediator _mediator;
    private readonly IFormFileSettingServices _fileSettingServices;

    public UsersController(IMediator mediator, IFormFileSettingServices fileSettingServices)
    {
        _mediator = mediator;
        _fileSettingServices = fileSettingServices;
    }

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] UserListFilterInput? filters)
    {
        filters ??= new UserListFilterInput();

        var status = filters.Status;
        var includeDeactivated = status is UserStatusFilter.All or UserStatusFilter.Inactive;
        var includeDeleted = status is UserStatusFilter.All or UserStatusFilter.Deleted;

        var normalizedFullName = UserFilterFormatting.Normalize(filters.FullName);
        var displayPhoneNumber = UserFilterFormatting.Normalize(filters.PhoneNumber);
        var normalizedPhoneNumber = UserFilterFormatting.NormalizePhoneNumber(filters.PhoneNumber);
        var normalizedRole = UserFilterFormatting.Normalize(filters.Role);

        var registeredFrom = UserFilterFormatting.ParsePersianDate(filters.RegisteredFrom, toExclusiveEnd: false, out var normalizedRegisteredFrom);
        var registeredTo = UserFilterFormatting.ParsePersianDate(filters.RegisteredTo, toExclusiveEnd: true, out var normalizedRegisteredTo);

        var filterCriteria = new UserFilterCriteria(
            includeDeactivated,
            includeDeleted,
            normalizedFullName,
            normalizedPhoneNumber,
            normalizedRole,
            status,
            registeredFrom,
            registeredTo);

        var query = new GetUsersQuery(filterCriteria);

        var result = await _mediator.Send(query);

        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error;
        }

        var users = result.IsSuccess
            ? result.Value!
            : Array.Empty<UserDto>();

        var roleLookup = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var userItems = users
            .Select(user =>
            {
                var roleNames = user.Roles
                    .Select(role => role.Name)
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .Select(name => name.Trim())
                    .ToArray();
                var roleDisplayNames = user.Roles
                    .Select(role => string.IsNullOrWhiteSpace(role.DisplayName) ? role.Name : role.DisplayName)
                    .Where(display => !string.IsNullOrWhiteSpace(display))
                    .Select(display => display!.Trim())
                    .ToArray();

                foreach (var role in user.Roles)
                {
                    if (!string.IsNullOrWhiteSpace(role.Name))
                    {
                        var key = role.Name.Trim();
                        var label = string.IsNullOrWhiteSpace(role.DisplayName) ? key : role.DisplayName.Trim();
                        roleLookup[key] = label;
                    }
                }

                return new UserListItemViewModel(
                    user.Id,
                    user.Email,
                    user.FullName,
                    user.IsActive,
                    user.IsDeleted,
                    user.DeactivatedOn,
                    user.DeletedOn,
                    user.LastModifiedOn,
                    user.PhoneNumber,
                    user.AvatarPath,
                    roleNames,
                    roleDisplayNames);
            })
            .ToArray();

        var totalCount = userItems.Length;
        var pageSize = filters.PageSize <= 0 ? DefaultPageSize : Math.Min(filters.PageSize, MaxPageSize);
        var pageNumber = filters.Page <= 0 ? 1 : filters.Page;
        var totalPages = pageSize <= 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);

        if (totalPages == 0)
        {
            pageNumber = 1;
        }
        else if (pageNumber > totalPages)
        {
            pageNumber = totalPages;
        }

        var skip = Math.Max(0, (pageNumber - 1) * pageSize);

        var pagedUsers = userItems
            .Skip(skip)
            .Take(pageSize)
            .ToArray();

        var availableRoles = roleLookup
            .OrderBy(pair => pair.Value, StringComparer.OrdinalIgnoreCase)
            .Select(pair => new RoleOptionViewModel(pair.Key, pair.Value))
            .ToList();

        if (!string.IsNullOrWhiteSpace(normalizedRole) && !roleLookup.ContainsKey(normalizedRole))
        {
            availableRoles.Insert(0, new RoleOptionViewModel(normalizedRole, normalizedRole));
        }

        var viewModel = new UserListViewModel
        {
            IncludeDeactivated = includeDeactivated,
            IncludeDeleted = includeDeleted,
            FullName = normalizedFullName,
            PhoneNumber = displayPhoneNumber,
            Role = normalizedRole,
            Status = status,
            RegisteredFrom = normalizedRegisteredFrom,
            RegisteredTo = normalizedRegisteredTo,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
            AvailableRoles = availableRoles,
            Users = pagedUsers
        };

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var model = new CreateUserViewModel();
        await PopulateRoleOptionsAsync(model);
        if (IsAjaxRequest())
        {
            return PartialView("_CreateUserModal", model);
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateUserViewModel model)
    {
        var normalizedPhone = UserFilterFormatting.NormalizePhoneNumber(model.PhoneNumber);

        model.SelectedRoles = NormalizeRoleSelections(model.SelectedRoles);
        await PopulateRoleOptionsAsync(model);

        if (!string.IsNullOrWhiteSpace(normalizedPhone))
        {
            if (normalizedPhone.StartsWith("0098", StringComparison.Ordinal))
            {
                normalizedPhone = normalizedPhone.Substring(4);
            }

            if (normalizedPhone.StartsWith("98", StringComparison.Ordinal) && normalizedPhone.Length >= 12)
            {
                normalizedPhone = normalizedPhone.Substring(2);
            }

            if (normalizedPhone.StartsWith("9", StringComparison.Ordinal) && normalizedPhone.Length == 10)
            {
                normalizedPhone = string.Concat("0", normalizedPhone);
            }
        }

        if (string.IsNullOrWhiteSpace(normalizedPhone))
        {
            ModelState.AddModelError(nameof(model.PhoneNumber), "وارد کردن شماره تماس الزامی است.");
        }
        else if (normalizedPhone.Length != 11 || !normalizedPhone.StartsWith("09", StringComparison.Ordinal))
        {
            ModelState.AddModelError(nameof(model.PhoneNumber), "شماره تماس باید با 09 شروع شده و 11 رقم باشد.");
        }

        var generatedUserName = !string.IsNullOrEmpty(normalizedPhone) && normalizedPhone.Length == 11
            ? string.Concat(normalizedPhone, "@gmail.com")
            : string.Empty;

        ValidateAvatar(model.Avatar, nameof(CreateUserViewModel.Avatar));

        if (!ModelState.IsValid)
        {
            if (IsAjaxRequest())
            {
                Response.StatusCode = StatusCodes.Status400BadRequest;
                return PartialView("_CreateUserModal", model);
            }

            return View(model);
        }

        var savedAvatarPath = await SaveAvatarAsync(model.Avatar, nameof(CreateUserViewModel.Avatar));

        if (!ModelState.IsValid)
        {
            if (IsAjaxRequest())
            {
                Response.StatusCode = StatusCodes.Status400BadRequest;
                return PartialView("_CreateUserModal", model);
            }

            return View(model);
        }

        var roles = model.SelectedRoles.ToArray();
        var payload = new RegisterUserDto(
            generatedUserName,
            model.Password,
            model.FullName,
            normalizedPhone!,
            roles,
            model.IsActive,
            model.IsActive ? null : model.DeactivationReason,
            savedAvatarPath);

        var result = await _mediator.Send(new RegisterUserCommand(payload));
        if (!result.IsSuccess)
        {
            if (!string.IsNullOrWhiteSpace(savedAvatarPath))
            {
                DeleteAvatarFile(savedAvatarPath);
            }

            ModelState.AddModelError(string.Empty, result.Error!);
            if (IsAjaxRequest())
            {
                Response.StatusCode = StatusCodes.Status400BadRequest;
                return PartialView("_CreateUserModal", model);
            }

            return View(model);
        }

        TempData["Success"] = "کاربر جدید با موفقیت ایجاد شد.";

        if (IsAjaxRequest())
        {
            return Json(new { success = true, redirectUrl = Url.Action(nameof(Index)) });
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return RedirectToAction(nameof(Index));
        }

        var result = await _mediator.Send(new GetUserByIdQuery(id));
        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error;
            return RedirectToAction(nameof(Index));
        }

        var user = result.Value!;
        var viewModel = new EditUserViewModel
        {
            Id = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            SelectedRoles = user.Roles.Select(role => role.Name).ToList(),
            IsActive = user.IsActive,
            IsDeleted = user.IsDeleted,
            PhoneNumber = user.PhoneNumber ?? string.Empty,
            AvatarPath = user.AvatarPath
        };

        await PopulateRoleOptionsAsync(viewModel);

        if (IsAjaxRequest())
        {
            return PartialView("_EditUserModal", viewModel);
        }

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, EditUserViewModel model)
    {
        if (!string.Equals(id, model.Id, StringComparison.Ordinal))
        {
            return BadRequest();
        }

        var normalizedPhone = UserFilterFormatting.NormalizePhoneNumber(model.PhoneNumber);
        if (!string.IsNullOrWhiteSpace(normalizedPhone))
        {
            if (normalizedPhone.StartsWith("0098", StringComparison.Ordinal))
            {
                normalizedPhone = normalizedPhone.Substring(4);
            }

            if (normalizedPhone.StartsWith("98", StringComparison.Ordinal) && normalizedPhone.Length >= 12)
            {
                normalizedPhone = normalizedPhone.Substring(2);
            }

            if (normalizedPhone.StartsWith("9", StringComparison.Ordinal) && normalizedPhone.Length == 10)
            {
                normalizedPhone = string.Concat("0", normalizedPhone);
            }

            if (normalizedPhone.Length > 11 && normalizedPhone.StartsWith("0", StringComparison.Ordinal))
            {
                normalizedPhone = normalizedPhone[^11..];
            }
        }

        var sanitizedPhone = normalizedPhone;

        if (string.IsNullOrWhiteSpace(sanitizedPhone))
        {
            ModelState.AddModelError(nameof(model.PhoneNumber), "وارد کردن شماره تماس الزامی است.");
        }
        else if (sanitizedPhone.Length != 11 || !sanitizedPhone.StartsWith("09", StringComparison.Ordinal))
        {
            ModelState.AddModelError(nameof(model.PhoneNumber), "شماره تماس باید با 09 شروع شده و 11 رقم باشد.");
        }

        if (!string.IsNullOrWhiteSpace(sanitizedPhone))
        {
            model.PhoneNumber = sanitizedPhone;
        }

        ValidateAvatar(model.Avatar, nameof(EditUserViewModel.Avatar));

        model.SelectedRoles = NormalizeRoleSelections(model.SelectedRoles);
        await PopulateRoleOptionsAsync(model);

        if (!ModelState.IsValid)
        {
            if (IsAjaxRequest())
            {
                Response.StatusCode = StatusCodes.Status400BadRequest;
                return PartialView("_EditUserModal", model);
            }

            return View(model);
        }

        if (model.IsDeleted)
        {
            ModelState.AddModelError(string.Empty, "کاربر حذف شده قابل ویرایش نیست.");
            if (IsAjaxRequest())
            {
                Response.StatusCode = StatusCodes.Status400BadRequest;
                return PartialView("_EditUserModal", model);
            }

            return View(model);
        }

        var roles = model.SelectedRoles.ToArray();
        var previousAvatarPath = model.AvatarPath;
        var uploadedAvatarPath = await SaveAvatarAsync(model.Avatar, nameof(EditUserViewModel.Avatar));

        if (!ModelState.IsValid)
        {
            if (IsAjaxRequest())
            {
                Response.StatusCode = StatusCodes.Status400BadRequest;
                return PartialView("_EditUserModal", model);
            }

            return View(model);
        }

        var nextAvatarPath = uploadedAvatarPath ?? previousAvatarPath;
        var payload = new UpdateUserDto(
            model.Id,
            model.Email,
            model.FullName,
            roles,
            model.IsActive,
            nextAvatarPath,
            model.PhoneNumber);

        var result = await _mediator.Send(new UpdateUserCommand(payload));
        if (!result.IsSuccess)
        {
            if (!string.IsNullOrWhiteSpace(uploadedAvatarPath))
            {
                DeleteAvatarFile(uploadedAvatarPath);
                model.AvatarPath = previousAvatarPath;
            }

            ModelState.AddModelError(string.Empty, result.Error!);
            if (IsAjaxRequest())
            {
                Response.StatusCode = StatusCodes.Status400BadRequest;
                return PartialView("_EditUserModal", model);
            }

            return View(model);
        }

        if (!string.IsNullOrWhiteSpace(uploadedAvatarPath) &&
            !string.IsNullOrWhiteSpace(previousAvatarPath) &&
            !string.Equals(previousAvatarPath, uploadedAvatarPath, StringComparison.OrdinalIgnoreCase))
        {
            DeleteAvatarFile(previousAvatarPath);
        }

        model.AvatarPath = nextAvatarPath;

        TempData["Success"] = "اطلاعات کاربر با موفقیت به‌روزرسانی شد.";

        if (IsAjaxRequest())
        {
            return Json(new { success = true, redirectUrl = Url.Action(nameof(Index)) });
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Deactivate(string id)
    {
        var viewModel = await LoadDeactivateViewModel(id);
        if (viewModel is null)
        {
            if (IsAjaxRequest())
            {
                return NotFound();
            }

            return RedirectToAction(nameof(Index));
        }

        if (IsAjaxRequest())
        {
            return PartialView("_DeactivateUserModal", viewModel);
        }

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(string id, DeactivateUserViewModel model)
    {
        if (!string.Equals(id, model.Id, StringComparison.Ordinal))
        {
            return BadRequest();
        }

        model.Reason = model.Reason?.Trim();

        if (!ModelState.IsValid)
        {
            if (IsAjaxRequest())
            {
                Response.StatusCode = StatusCodes.Status400BadRequest;
                return PartialView("_DeactivateUserModal", model);
            }

            return View(model);
        }

        var result = await _mediator.Send(new DeactivateUserCommand(model.Id, model.Reason));
        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            if (IsAjaxRequest())
            {
                Response.StatusCode = StatusCodes.Status400BadRequest;
                return PartialView("_DeactivateUserModal", model);
            }

            return View(model);
        }

        TempData["Success"] = "کاربر با موفقیت غیرفعال شد.";
        if (IsAjaxRequest())
        {
            return Json(new { success = true, redirectUrl = Url.Action(nameof(Index)) });
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Activate(string id)
    {
        var viewModel = await LoadActivateViewModel(id);
        if (viewModel is null)
        {
            if (IsAjaxRequest())
            {
                return NotFound();
            }

            return RedirectToAction(nameof(Index));
        }

        if (IsAjaxRequest())
        {
            return PartialView("_ActivateUserModal", viewModel);
        }

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Activate(string id, ActivateUserViewModel model)
    {
        if (!string.Equals(id, model.Id, StringComparison.Ordinal))
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            if (IsAjaxRequest())
            {
                Response.StatusCode = StatusCodes.Status400BadRequest;
                return PartialView("_ActivateUserModal", model);
            }

            return View(model);
        }

        var command = new UpdateUserCommand(new UpdateUserDto(model.Id, null, null, null, true));
        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Error!);

            if (IsAjaxRequest())
            {
                Response.StatusCode = StatusCodes.Status400BadRequest;
                return PartialView("_ActivateUserModal", model);
            }

            TempData["Error"] = result.Error;
            return View(model);
        }

        TempData["Success"] = "کاربر با موفقیت فعال شد.";

        if (IsAjaxRequest())
        {
            return Json(new { success = true, redirectUrl = Url.Action(nameof(Index)) });
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Delete(string id)
    {
        var viewModel = await LoadDeleteViewModel(id);
        if (viewModel is null)
        {
            if (IsAjaxRequest())
            {
                return NotFound();
            }

            return RedirectToAction(nameof(Index));
        }

        if (IsAjaxRequest())
        {
            return PartialView("_DeleteUserModal", viewModel);
        }

        return View(viewModel);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(string id)
    {
        var viewModel = await LoadDeleteViewModel(id);
        if (viewModel is null)
        {
            if (IsAjaxRequest())
            {
                return NotFound();
            }

            return RedirectToAction(nameof(Index));
        }

        var result = await _mediator.Send(new SoftDeleteUserCommand(viewModel.Id));
        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "حذف کاربر با خطا مواجه شد.");

            if (IsAjaxRequest())
            {
                Response.StatusCode = StatusCodes.Status400BadRequest;
                return PartialView("_DeleteUserModal", viewModel);
            }

            TempData["Error"] = result.Error;
            return RedirectToAction(nameof(Delete), new { id });
        }

        TempData["Success"] = "کاربر با موفقیت حذف شد.";

        if (IsAjaxRequest())
        {
            return Json(new { success = true, redirectUrl = Url.Action(nameof(Index)) });
        }

        return RedirectToAction(nameof(Index));
    }

    private bool ValidateAvatar(IFormFile? avatarFile, string propertyName)
    {
        if (avatarFile is null || avatarFile.Length == 0)
        {
            return true;
        }

        if (!_fileSettingServices.IsFileSizeValid(avatarFile, MaxAvatarFileSizeKb))
        {
            ModelState.AddModelError(propertyName, "حجم تصویر باید کمتر از ۲ مگابایت باشد.");
            return false;
        }

        var contentType = avatarFile.ContentType ?? string.Empty;
        if (!AllowedAvatarContentTypes.Contains(contentType))
        {
            ModelState.AddModelError(propertyName, "فقط فرمت‌های PNG، JPG و WEBP پشتیبانی می‌شوند.");
            return false;
        }

        return true;
    }

    private Task<string?> SaveAvatarAsync(IFormFile? avatarFile, string propertyName)
    {
        if (avatarFile is null || avatarFile.Length == 0)
        {
            return Task.FromResult<string?>(null);
        }

        try
        {
            var response = _fileSettingServices.UploadImage(AvatarUploadFolder, avatarFile, Guid.NewGuid().ToString("N"));

            if (!response.Success)
            {
                var errorMessage = response.Messages.FirstOrDefault()?.message ?? "ذخیره‌سازی فایل انجام نشد.";
                ModelState.AddModelError(propertyName, errorMessage);
                return Task.FromResult<string?>(null);
            }

            return Task.FromResult(response.Data);
        }
        catch
        {
            ModelState.AddModelError(propertyName, "ذخیره‌سازی فایل انجام نشد.");
            return Task.FromResult<string?>(null);
        }
    }

    private void DeleteAvatarFile(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return;
        }

        _fileSettingServices.DeleteFile(relativePath);
    }

    private bool IsAjaxRequest()
        => Request.Headers.TryGetValue("X-Requested-With", out var value) &&
           StringValues.Equals(value, "XMLHttpRequest");

    private async Task PopulateRoleOptionsAsync(CreateUserViewModel model)
    {
        var options = await BuildRoleOptionsAsync(model.SelectedRoles);
        model.AvailableRoles = options;
    }

    private async Task PopulateRoleOptionsAsync(EditUserViewModel model)
    {
        var options = await BuildRoleOptionsAsync(model.SelectedRoles);
        model.AvailableRoles = options;
    }

    private async Task<IReadOnlyCollection<RoleOptionViewModel>> BuildRoleOptionsAsync(IEnumerable<string>? selectedRoles)
    {
        var normalizedSelections = NormalizeRoleSelections(selectedRoles);
        var result = await _mediator.Send(new GetAllRolesQuery());

        if (!result.IsSuccess && !string.IsNullOrWhiteSpace(result.Error))
        {
            TempData["Error"] = result.Error;
        }

        var options = result.IsSuccess
            ? result.Value!
                .Select(role => new RoleOptionViewModel(role.Name, string.IsNullOrWhiteSpace(role.DisplayName) ? role.Name : role.DisplayName))
                .ToList()
            : new List<RoleOptionViewModel>();

        foreach (var role in normalizedSelections)
        {
            if (!options.Any(option => string.Equals(option.Value, role, StringComparison.OrdinalIgnoreCase)))
            {
                options.Add(new RoleOptionViewModel(role, role));
            }
        }

        return options
            .OrderBy(option => option.Label, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static List<string> NormalizeRoleSelections(IEnumerable<string>? roles)
    {
        if (roles is null)
        {
            return new List<string>();
        }

        return roles
            .Where(role => !string.IsNullOrWhiteSpace(role))
            .Select(role => role.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private async Task<DeactivateUserViewModel?> LoadDeactivateViewModel(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        var result = await _mediator.Send(new GetUserByIdQuery(id));
        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error;
            return null;
        }

        var user = result.Value!;
        if (!user.IsActive)
        {
            TempData["Error"] = "این کاربر از قبل غیرفعال شده است.";
            return null;
        }

        if (user.IsDeleted)
        {
            TempData["Error"] = "کاربر حذف شده قابل غیرفعال سازی نیست.";
            return null;
        }

        return new DeactivateUserViewModel
        {
            Id = user.Id,
            Email = user.Email,
            FullName = user.FullName
        };
    }

    private async Task<DeleteUserViewModel?> LoadDeleteViewModel(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        var result = await _mediator.Send(new GetUserByIdQuery(id));
        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error;
            return null;
        }

        var user = result.Value!;
        if (user.IsDeleted)
        {
            TempData["Error"] = "کاربر موردنظر قبلاً حذف شده است.";
            return null;
        }

        return new DeleteUserViewModel
        {
            Id = user.Id,
            Email = user.Email,
            FullName = user.FullName
        };
    }

    private async Task<ActivateUserViewModel?> LoadActivateViewModel(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        var result = await _mediator.Send(new GetUserByIdQuery(id));
        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error;
            return null;
        }

        var user = result.Value!;
        if (user.IsDeleted)
        {
            TempData["Error"] = "کاربر حذف شده قابل فعال‌سازی نیست.";
            return null;
        }

        if (user.IsActive)
        {
            TempData["Error"] = "این کاربر از قبل فعال است.";
            return null;
        }

        return new ActivateUserViewModel
        {
            Id = user.Id,
            Email = user.Email,
            FullName = user.FullName
        };
    }
}

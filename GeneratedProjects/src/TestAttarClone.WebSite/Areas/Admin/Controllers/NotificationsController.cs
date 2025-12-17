using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using TestAttarClone.Application.Commands.Notifications;
using TestAttarClone.Application.DTOs.Notifications;
using TestAttarClone.Application.Queries.Identity.GetRoles;
using TestAttarClone.Application.Queries.Identity.GetUsers;
using TestAttarClone.Application.Queries.Identity.GetUserLookups;
using TestAttarClone.Application.Queries.Notifications;
using TestAttarClone.Domain.Enums;
using TestAttarClone.SharedKernel.Extensions;
using TestAttarClone.WebSite.Areas.Admin.Models;
using TestAttarClone.WebSite.App;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace TestAttarClone.WebSite.Areas.Admin.Controllers;

[Area("Admin")]
public sealed class NotificationsController : Controller
{
    private readonly IMediator _mediator;

    public NotificationsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> List(
        string? searchTitle = null,
        string? searchMessage = null,
        int? typeFilter = null,
        int? priorityFilter = null,
        string? dateFromFilter = null,
        string? dateToFilter = null,
        bool? isActiveFilter = null)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            TempData["Error"] = "کاربر یافت نشد.";
            return View(new AdminNotificationListViewModel());
        }

        ViewData["Title"] = "اعلان‌های ارسالی";
        ViewData["Subtitle"] = "مدیریت اعلان‌های ارسال شده توسط شما";

        DateTimeOffset? dateFrom = null;
        DateTimeOffset? dateTo = null;

        if (!string.IsNullOrWhiteSpace(dateFromFilter))
        {
            var parsedDate = UserFilterFormatting.ParsePersianDate(dateFromFilter, toExclusiveEnd: false, out _);
            if (parsedDate.HasValue)
            {
                dateFrom = parsedDate;
            }
        }

        if (!string.IsNullOrWhiteSpace(dateToFilter))
        {
            var parsedDate = UserFilterFormatting.ParsePersianDate(dateToFilter, toExclusiveEnd: true, out _);
            if (parsedDate.HasValue)
            {
                dateTo = parsedDate;
            }
        }

        var cancellationToken = HttpContext.RequestAborted;
        var result = await _mediator.Send(
            new GetAdminCreatedNotificationsQuery(
                userId,
                searchTitle,
                searchMessage,
                typeFilter,
                priorityFilter,
                dateFrom,
                dateTo,
                isActiveFilter),
            cancellationToken);

        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error ?? "خطا در دریافت اعلان‌ها.";
            return View(new AdminNotificationListViewModel());
        }

        var dto = result.Value!;
        var model = new AdminNotificationListViewModel
        {
            Items = dto.Items.Select(n => new AdminNotificationItemViewModel
            {
                Id = n.Id,
                Title = n.Title,
                Message = n.Message,
                Type = n.Type,
                Priority = n.Priority,
                SentAt = n.SentAt,
                ExpiresAt = n.ExpiresAt,
                IsActive = n.IsActive,
                RecipientCount = n.RecipientCount,
                IsExpired = n.ExpiresAt.HasValue && n.ExpiresAt.Value < DateTimeOffset.UtcNow
            }).ToList(),
            Stats = new AdminNotificationStatsViewModel
            {
                Total = dto.Stats.Total,
                Active = dto.Stats.Active,
                Inactive = dto.Stats.Inactive,
                Expired = dto.Stats.Expired
            },
            SearchTitle = searchTitle,
            SearchMessage = searchMessage,
            TypeFilter = typeFilter,
            PriorityFilter = priorityFilter,
            DateFromFilterPersian = dateFromFilter,
            DateToFilterPersian = dateToFilter,
            IsActiveFilter = isActiveFilter
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, AdminEditNotificationViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Json(new { success = false, error = "کاربر یافت نشد." });
        }

        DateTimeOffset? expiresAt = null;
        if (!string.IsNullOrWhiteSpace(model.ExpiresAtPersian))
        {
            var parsedDate = UserFilterFormatting.ParsePersianDate(model.ExpiresAtPersian, toExclusiveEnd: false, out _);
            if (parsedDate.HasValue)
            {
                expiresAt = parsedDate;
            }
        }
        else if (model.ExpiresAt.HasValue)
        {
            expiresAt = new DateTimeOffset(model.ExpiresAt.Value);
        }

        var cancellationToken = HttpContext.RequestAborted;
        var result = await _mediator.Send(
            new UpdateNotificationCommand(id, model.Title, model.Message, expiresAt),
            cancellationToken);

        if (result.IsSuccess)
        {
            return Json(new { success = true });
        }

        return Json(new { success = false, error = result.Error });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Json(new { success = false, error = "کاربر یافت نشد." });
        }

        var cancellationToken = HttpContext.RequestAborted;
        var result = await _mediator.Send(
            new DeleteNotificationCommand(id),
            cancellationToken);

        if (result.IsSuccess)
        {
            return Json(new { success = true });
        }

        return Json(new { success = false, error = result.Error });
    }

    [HttpGet]
    public async Task<IActionResult> Index(bool? isRead = null)

    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            TempData["Error"] = "کاربر یافت نشد.";
            return View(new NotificationListViewModel { Items = Array.Empty<NotificationItemViewModel>() });
        }

        ViewData["Title"] = "اعلان‌ها";
        ViewData["Subtitle"] = "مدیریت اعلان‌های سیستم";

        var cancellationToken = HttpContext.RequestAborted;
        var result = await _mediator.Send(new GetUserNotificationsQuery(userId, "Admin", isRead), cancellationToken);

        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error ?? "خطا در دریافت اعلان‌ها.";
            return View(new NotificationListViewModel { Items = Array.Empty<NotificationItemViewModel>() });
        }

        var dto = result.Value!;

        // Admin panel should show only new ticket notifications
        var filtered = dto.Items
            .Where(n => string.Equals(n.Title, "تیکت جدید", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var model = new NotificationListViewModel
        {
            Items = filtered.Select(n => new NotificationItemViewModel
            {
                Id = n.Id,
                Title = n.Title,
                Message = n.Message,
                Type = n.Type,
                Priority = n.Priority,
                SentAt = n.SentAt,
                ExpiresAt = n.ExpiresAt,
                CreatedByDisplayName = n.CreatedByDisplayName,
                IsRead = n.IsRead,
                ReadAt = n.ReadAt
            }).ToList(),
            TotalCount = filtered.Count,
            UnreadCount = filtered.Count(n => !n.IsRead),
            IsReadFilter = isRead
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAsRead(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Json(new { success = false, error = "کاربر یافت نشد." });
        }

        var cancellationToken = HttpContext.RequestAborted;
        var result = await _mediator.Send(new MarkNotificationAsReadCommand(id, userId), cancellationToken);

        if (result.IsSuccess)
        {
            return Json(new { success = true });
        }

        return Json(new { success = false, error = result.Error });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            TempData["Error"] = "کاربر یافت نشد.";
            return RedirectToAction(nameof(Index));
        }

        var cancellationToken = HttpContext.RequestAborted;
        var result = await _mediator.Send(new MarkAllNotificationsAsReadCommand(userId), cancellationToken);

        if (result.IsSuccess)
        {
            TempData["Success"] = "همه اعلان‌ها به عنوان خوانده شده علامت‌گذاری شدند.";
            return RedirectToAction(nameof(Index));
        }

        TempData["Error"] = result.Error ?? "خطا در به‌روزرسانی اعلان‌ها.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var model = new CreateNotificationViewModel();
        await PopulateFormOptionsAsync(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateNotificationViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateFormOptionsAsync(model);
            return View(model);
        }

        var cancellationToken = HttpContext.RequestAborted;
        DateTimeOffset? expiresAt = null;

        if (!string.IsNullOrWhiteSpace(model.ExpiresAtPersian))
        {
            var parsedDate = UserFilterFormatting.ParsePersianDate(model.ExpiresAtPersian, toExclusiveEnd: false, out _);
            if (parsedDate.HasValue)
            {
                expiresAt = parsedDate;
            }
        }
        else if (model.ExpiresAt.HasValue)
        {
            expiresAt = model.ExpiresAt.Value;
        }

        DateTimeOffset? registeredFrom = null;
        DateTimeOffset? registeredTo = null;

        if (!string.IsNullOrWhiteSpace(model.RegisteredFromPersian))
        {
            var fromDate = UserFilterFormatting.ParsePersianDate(model.RegisteredFromPersian, toExclusiveEnd: false, out _);
            if (fromDate.HasValue)
            {
                registeredFrom = fromDate;
            }
        }

        if (!string.IsNullOrWhiteSpace(model.RegisteredToPersian))
        {
            var toDate = UserFilterFormatting.ParsePersianDate(model.RegisteredToPersian, toExclusiveEnd: true, out _);
            if (toDate.HasValue)
            {
                registeredTo = toDate;
            }
        }

        var filter = new NotificationFilterDto(
            model.SelectedRoles?.Where(r => !string.IsNullOrWhiteSpace(r)).ToList(),
            registeredFrom,
            registeredTo,
            model.SelectedUserIds?.Where(id => !string.IsNullOrWhiteSpace(id)).ToList());

        var dto = new CreateNotificationDto(
            model.Title,
            model.Message,
            model.Type,
            model.Priority,
            expiresAt,
            filter);

        var result = await _mediator.Send(new CreateNotificationCommand(dto), cancellationToken);

        if (result.IsSuccess)
        {
            TempData["Success"] = "اعلان با موفقیت ارسال شد.";
            return RedirectToAction(nameof(Create));
        }

        ModelState.AddModelError(string.Empty, result.Error ?? "خطا در ارسال اعلان.");
        await PopulateFormOptionsAsync(model);
        return View(model);
    }

    private async Task PopulateFormOptionsAsync(CreateNotificationViewModel model)
    {
        var cancellationToken = HttpContext.RequestAborted;

        // Populate roles
        var rolesResult = await _mediator.Send(new GetAllRolesQuery(), cancellationToken);
        if (rolesResult.IsSuccess && rolesResult.Value is not null)
        {
            model.AvailableRoles = rolesResult.Value
                .Select(role => new SelectListItem(
                    string.IsNullOrWhiteSpace(role.DisplayName) ? role.Name : role.DisplayName,
                    role.Name,
                    model.SelectedRoles?.Contains(role.Name) == true))
                .ToList();
        }

        // Populate notification types
        model.TypeOptions = Enum.GetValues<NotificationType>()
            .Select(t => new SelectListItem(t.GetDisplayName(), ((int)t).ToString(), t == model.Type))
            .ToList();

        // Populate priorities
        model.PriorityOptions = Enum.GetValues<NotificationPriority>()
            .Select(p => new SelectListItem(p.GetDisplayName(), ((int)p).ToString(), p == model.Priority))
            .ToList();

        // Populate users
        var usersResult = await _mediator.Send(new GetUserLookupsQuery(500), cancellationToken);
        if (usersResult.IsSuccess && usersResult.Value is not null)
        {
            var selectedUserIdsSet = new HashSet<string>(
                model.SelectedUserIds?.Where(id => !string.IsNullOrWhiteSpace(id)) ?? Array.Empty<string>(),
                StringComparer.OrdinalIgnoreCase);

            model.AvailableUsers = usersResult.Value
                .Select(user => new SelectListItem(
                    $"{user.DisplayName} ({user.Email})",
                    user.Id,
                    selectedUserIdsSet.Contains(user.Id)))
                .OrderBy(item => item.Text)
                .ToList();
        }
    }
}


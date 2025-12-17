using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TestAttarClone.Application.Commands.Notifications;
using TestAttarClone.Application.Queries.Notifications;
using TestAttarClone.Domain.Enums;
using TestAttarClone.SharedKernel.Authorization;
using TestAttarClone.WebSite.Areas.Seller.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace TestAttarClone.WebSite.Areas.Seller.Controllers;

[Area("Seller")]
[Authorize(Policy = AuthorizationPolicies.SellerPanelAccess)]
public sealed class NotificationsController : Controller
{
    private readonly IMediator _mediator;

    public NotificationsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        bool? isRead = null,
        NotificationType? type = null,
        NotificationPriority? priority = null,
        string? search = null,
        DateTimeOffset? fromDate = null,
        DateTimeOffset? toDate = null,
        int page = 1,
        int pageSize = 10)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return RedirectToAction("Index", "Dashboard");
        }

        var cancellationToken = HttpContext.RequestAborted;
        var result = await _mediator.Send(new GetUserNotificationsQuery(userId, "Seller", isRead), cancellationToken);

        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error ?? "خطا در دریافت اعلان‌ها.";
            return View(new NotificationListViewModel { Items = Array.Empty<NotificationItemViewModel>() });
        }

        var dto = result.Value!;

        // Seller panel: show only notifications that were sent by admins
        var roleFilteredItems = dto.Items
            .Where(n => n.CreatedByIsAdmin)
            .ToList();

        var filteredItems = roleFilteredItems;

        if (!string.IsNullOrWhiteSpace(search))
        {
            filteredItems = filteredItems
                .Where(n =>
                    (!string.IsNullOrWhiteSpace(n.Title) && n.Title.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrWhiteSpace(n.Message) && n.Message.Contains(search, StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }

        if (type.HasValue)
        {
            filteredItems = filteredItems.Where(n => n.Type == type.Value).ToList();
        }

        if (priority.HasValue)
        {
            filteredItems = filteredItems.Where(n => n.Priority == priority.Value).ToList();
        }

        if (fromDate.HasValue)
        {
            filteredItems = filteredItems.Where(n => n.SentAt >= fromDate.Value).ToList();
        }

        if (toDate.HasValue)
        {
            var toDateExclusive = toDate.Value.Date.AddDays(1);
            filteredItems = filteredItems.Where(n => n.SentAt < toDateExclusive).ToList();
        }

        pageSize = Math.Clamp(pageSize, 5, 50);
        page = page < 1 ? 1 : page;

        var totalCount = filteredItems.Count;
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        if (totalPages > 0 && page > totalPages)
        {
            page = totalPages;
        }

        var pagedItems = filteredItems
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();
        
        var model = new NotificationListViewModel
        {
            Items = pagedItems.Select(n => new NotificationItemViewModel
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
            TotalCount = totalCount,
            UnreadCount = filteredItems.Count(n => !n.IsRead),
            IsReadFilter = isRead,
            PageNumber = page,
            PageSize = pageSize,
            TotalPages = totalPages,
            Filter = new NotificationFilterViewModel
            {
                Type = type,
                Priority = priority,
                Search = search,
                FromDate = fromDate,
                ToDate = toDate,
                IsRead = isRead
            }
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAsRead(System.Guid id)
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
            return Json(new { success = false, error = "کاربر یافت نشد." });
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
}


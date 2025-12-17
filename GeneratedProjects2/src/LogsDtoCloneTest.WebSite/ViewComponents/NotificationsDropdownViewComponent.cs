using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using LogsDtoCloneTest.Application.Queries.Notifications;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace LogsDtoCloneTest.WebSite.ViewComponents;

public sealed class NotificationsDropdownViewComponent : ViewComponent
{
    private readonly IMediator _mediator;

    public NotificationsDropdownViewComponent(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        if (!ViewContext.HttpContext.User.Identity?.IsAuthenticated ?? true)
        {
            return View(new NotificationsDropdownViewModel(0, Array.Empty<NotificationDropdownItemViewModel>()));
        }

        var userId = ViewContext.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return View(new NotificationsDropdownViewModel(0, Array.Empty<NotificationDropdownItemViewModel>()));
        }

        // Check area to filter notifications appropriately
        var area = ViewContext.RouteData.Values["area"]?.ToString();
        var isUserArea = string.Equals(area, "User", StringComparison.OrdinalIgnoreCase);
        var isSellerArea = string.Equals(area, "Seller", StringComparison.OrdinalIgnoreCase);
        var isAdminArea = string.Equals(area, "Admin", StringComparison.OrdinalIgnoreCase);

        // Determine the user role for filtering notifications
        string? userRole = null;
        if (isUserArea)
            userRole = "User";
        else if (isSellerArea)
            userRole = "Seller";
        else if (isAdminArea)
            userRole = "Admin";

        // Get recent notifications (last 5 unread or all if less than 5)
        var notificationsResult = await _mediator.Send(new GetUserNotificationsQuery(userId, userRole, IsRead: false));
        var recentNotifications = Array.Empty<NotificationDropdownItemViewModel>();
        var unreadCount = 0;

        if (notificationsResult.IsSuccess && notificationsResult.Value is not null)
        {
            var dto = notificationsResult.Value;

            // Filter notifications based on area and new display rules
            var filteredList = dto.Items.ToList();

            if (isUserArea)
            {
                // User panel: show ticket replies and admin-sent notifications
                filteredList = filteredList
                    .Where(n => string.Equals(n.Title, "پاسخ به تیکت", StringComparison.OrdinalIgnoreCase)
                             || n.CreatedByIsAdmin)
                    .ToList();
            }
            else if (isSellerArea)
            {
                // Seller panel: show only admin-sent notifications
                filteredList = filteredList
                    .Where(n => n.CreatedByIsAdmin)
                    .ToList();
            }
            else
            {
                // Default: show admin-sent notifications and ticket replies
                filteredList = filteredList
                    .Where(n => n.CreatedByIsAdmin || string.Equals(n.Title, "پاسخ به تیکت", StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            unreadCount = filteredList.Count(n => !n.IsRead);

            recentNotifications = filteredList
                .Take(5)
                .Select(n => new NotificationDropdownItemViewModel(
                    n.Id,
                    n.Title,
                    n.Message,
                    n.Type,
                    n.Priority,
                    n.SentAt,
                    n.IsRead))
                .ToArray();
        }

        var model = new NotificationsDropdownViewModel(unreadCount, recentNotifications);
        return View(model);
    }
}

public record NotificationsDropdownViewModel(
    int UnreadCount,
    IReadOnlyCollection<NotificationDropdownItemViewModel> RecentNotifications);

public record NotificationDropdownItemViewModel(
    Guid Id,
    string Title,
    string Message,
    Domain.Enums.NotificationType Type,
    Domain.Enums.NotificationPriority Priority,
    DateTimeOffset SentAt,
    bool IsRead);


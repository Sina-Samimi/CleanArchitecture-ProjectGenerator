using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using LogTableRenameTest.Application.Queries.Notifications;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace LogTableRenameTest.WebSite.ViewComponents;

public sealed class UnreadNotificationsCountViewComponent : ViewComponent
{
    private readonly IMediator _mediator;

    public UnreadNotificationsCountViewComponent(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        if (!ViewContext.HttpContext.User.Identity?.IsAuthenticated ?? true)
        {
            return View(0);
        }

        var userId = ViewContext.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return View(0);
        }


        // Check area to apply same filtering as dropdown
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

        // Get all unread notifications
        var notificationsResult = await _mediator.Send(new GetUserNotificationsQuery(userId, userRole, IsRead: false));

        if (!notificationsResult.IsSuccess || notificationsResult.Value is null)
        {
            return View(0);
        }

        var notifications = notificationsResult.Value.Items.ToList();

        if (isUserArea)
        {
            // User: show ticket replies and admin-sent notifications
            notifications = notifications
                .Where(n => string.Equals(n.Title, "پاسخ به تیکت", StringComparison.OrdinalIgnoreCase) || n.CreatedByIsAdmin)
                .ToList();
        }
        else if (isSellerArea)
        {
            // Seller: show only admin-sent notifications
            notifications = notifications
                .Where(n => n.CreatedByIsAdmin)
                .ToList();
        }

        var count = notifications.Count;

        return View(count);
    }
}


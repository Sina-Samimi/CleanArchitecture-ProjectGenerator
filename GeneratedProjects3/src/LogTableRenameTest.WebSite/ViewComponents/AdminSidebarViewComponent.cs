using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using LogTableRenameTest.Application.Queries.Billing;
using LogTableRenameTest.Application.Queries.Catalog;
using LogTableRenameTest.Application.Queries.Notifications;
using LogTableRenameTest.Application.Queries.Orders;
using LogTableRenameTest.Application.Queries.Tickets;
using LogTableRenameTest.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace LogTableRenameTest.WebSite.ViewComponents;

public class AdminSidebarViewComponent : ViewComponent
{
    private readonly IMediator _mediator;
    private readonly UserManager<ApplicationUser> _userManager;

    public AdminSidebarViewComponent(IMediator mediator, UserManager<ApplicationUser> userManager)
    {
        _mediator = mediator;
        _userManager = userManager;
    }

    public async Task<IViewComponentResult> InvokeAsync(string? currentArea, string? currentController, string? currentAction)
    {
        var area = !string.IsNullOrWhiteSpace(currentArea)
            ? currentArea
            : ViewContext.RouteData.Values["area"]?.ToString();

        var userId = ViewContext.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        
        // Get admin user information
        string accountName = "مدیر سیستم";
        string accountInitial = "م";
        string? accountAvatarUrl = null;
        string? accountEmail = null;
        string? accountPhone = null;

        if (!string.IsNullOrWhiteSpace(userId))
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user is not null)
                {
                    accountName = !string.IsNullOrWhiteSpace(user.FullName) ? user.FullName : (user.UserName ?? "مدیر سیستم");
                    accountInitial = accountName.Trim().Length > 0 ? accountName.Trim()[0].ToString() : "م";
                    accountAvatarUrl = user.AvatarPath;
                    accountEmail = user.Email;
                    accountPhone = user.PhoneNumber;
                    
                    // Set ViewData for use in layout
                    ViewContext.ViewData["AccountName"] = accountName;
                    ViewContext.ViewData["AccountInitial"] = accountInitial;
                    if (!string.IsNullOrWhiteSpace(accountAvatarUrl))
                    {
                        ViewContext.ViewData["AccountAvatarUrl"] = accountAvatarUrl;
                    }
                    if (!string.IsNullOrWhiteSpace(accountEmail))
                    {
                        ViewContext.ViewData["AccountEmail"] = accountEmail;
                        ViewContext.ViewData["Sidebar:Email"] = accountEmail;
                    }
                    if (!string.IsNullOrWhiteSpace(accountPhone))
                    {
                        ViewContext.ViewData["AccountPhone"] = accountPhone;
                        ViewContext.ViewData["Sidebar:Phone"] = accountPhone;
                    }
                }
            }
            catch
            {
                // Ignore errors, use defaults
            }
        }

        // Override with ViewData if provided
        accountName = ViewContext.ViewData["AccountName"] as string ?? accountName;
        accountInitial = ViewContext.ViewData["AccountInitial"] as string ?? accountInitial;
        accountAvatarUrl = ViewContext.ViewData["AccountAvatarUrl"] as string ?? accountAvatarUrl;
        accountEmail = ViewContext.ViewData["AccountEmail"] as string ?? ViewContext.ViewData["Sidebar:Email"] as string ?? accountEmail;
        accountPhone = ViewContext.ViewData["AccountPhone"] as string ?? ViewContext.ViewData["Sidebar:Phone"] as string ?? accountPhone;
        var profileCompletion = ReadPercent(ViewContext.ViewData["Sidebar:Completion"]) ??
                                ReadPercent(ViewContext.ViewData["ProfileCompletion"]) ??
                                ReadPercent(ViewContext.ViewData["ProfileCompletionPercent"]);
        var greetingSubtitle = ViewContext.ViewData["GreetingSubtitle"] as string;
        var activeTab = ViewContext.ViewData["Sidebar:ActiveTab"] as string;

        // Get new orders count
        var newOrdersCount = 0;
        try
        {
            var countResult = await _mediator.Send(new GetNewOrdersCountQuery(), HttpContext.RequestAborted);
            if (countResult.IsSuccess)
            {
                newOrdersCount = countResult.Value;
            }
        }
        catch
        {
            // Ignore errors in sidebar
        }

        // Get pending withdrawal requests count
        var pendingWithdrawalRequestsCount = 0;
        try
        {
            var withdrawalCountResult = await _mediator.Send(new GetPendingWithdrawalRequestsCountQuery(), HttpContext.RequestAborted);
            if (withdrawalCountResult.IsSuccess)
            {
                pendingWithdrawalRequestsCount = withdrawalCountResult.Value;
            }
        }
        catch
        {
            // Ignore errors in sidebar
        }

        // Get new tickets count
        var newTicketsCount = 0;
        try
        {
            var ticketsCountResult = await _mediator.Send(new GetNewTicketsCountQuery(), HttpContext.RequestAborted);
            if (ticketsCountResult.IsSuccess)
            {
                newTicketsCount = ticketsCountResult.Value;
            }
        }
        catch
        {
            // Ignore errors in sidebar
        }

        // Get pending product custom requests count
        var pendingProductCustomRequestsCount = 0;
        try
        {
            var customRequestsCountResult = await _mediator.Send(new GetPendingProductCustomRequestsCountQuery(), HttpContext.RequestAborted);
            if (customRequestsCountResult.IsSuccess)
            {
                pendingProductCustomRequestsCount = customRequestsCountResult.Value;
            }
        }
        catch
        {
            // Ignore errors in sidebar
        }

        // Get pending product requests count
        var pendingProductRequestsCount = 0;
        try
        {
            var productRequestsCountResult = await _mediator.Send(new GetPendingProductRequestsCountQuery(), HttpContext.RequestAborted);
            if (productRequestsCountResult.IsSuccess)
            {
                pendingProductRequestsCount = productRequestsCountResult.Value;
            }
        }
        catch
        {
            // Ignore errors in sidebar
        }

        // Get unpublished product offers count
        var unpublishedProductOffersCount = 0;
        try
        {
            var offersCountResult = await _mediator.Send(new GetUnpublishedProductOffersCountQuery(), HttpContext.RequestAborted);
            if (offersCountResult.IsSuccess)
            {
                unpublishedProductOffersCount = offersCountResult.Value;
            }
        }
        catch
        {
            // Ignore errors in sidebar
        }

        // Get unread notifications count
        var unreadNotificationsCount = 0;
        if (!string.IsNullOrWhiteSpace(userId))
        {
            try
            {
                var notificationsCountResult = await _mediator.Send(new GetUnreadNotificationsCountQuery(userId, "Admin"), HttpContext.RequestAborted);
                if (notificationsCountResult.IsSuccess)
                {
                    unreadNotificationsCount = notificationsCountResult.Value;
                }
            }
            catch
            {
                // Ignore errors in sidebar
            }
        }

        var model = new AdminSidebarViewModel(
            area,
            currentController,
            currentAction,
            accountName!,
            accountInitial!,
            accountAvatarUrl,
            accountEmail,
            accountPhone,
            profileCompletion,
            greetingSubtitle,
            NormalizeTabKey(activeTab),
            newOrdersCount,
            pendingWithdrawalRequestsCount,
            newTicketsCount,
            pendingProductCustomRequestsCount,
            pendingProductRequestsCount,
            unpublishedProductOffersCount,
            unreadNotificationsCount);

        return View(model);
    }

    private static int? ReadPercent(object? value)
    {
        return value switch
        {
            null => null,
            int i => Math.Clamp(i, 0, 100),
            double d => Math.Clamp((int)Math.Round(d, MidpointRounding.AwayFromZero), 0, 100),
            float f => Math.Clamp((int)Math.Round(f, MidpointRounding.AwayFromZero), 0, 100),
            string s when int.TryParse(s, out var parsed) => Math.Clamp(parsed, 0, 100),
            _ => null
        };
    }

    private static string? NormalizeTabKey(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "settings";
        }

        return value.Trim();
    }
}

public record AdminSidebarViewModel(
    string? CurrentArea,
    string? CurrentController,
    string? CurrentAction,
    string AccountName,
    string AccountInitial,
    string? AccountAvatarUrl,
    string? AccountEmail,
    string? AccountPhone,
    int? ProfileCompletionPercent,
    string? GreetingSubtitle,
    string? ActiveTab,
    int NewOrdersCount = 0,
    int PendingWithdrawalRequestsCount = 0,
    int NewTicketsCount = 0,
    int PendingProductCustomRequestsCount = 0,
    int PendingProductRequestsCount = 0,
    int UnpublishedProductOffersCount = 0,
    int UnreadNotificationsCount = 0);

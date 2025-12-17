using System;
using System.Security.Claims;
using System.Threading.Tasks;
using LogTableRenameTest.Application.Queries.Notifications;
using LogTableRenameTest.Application.Queries.Tickets;
using LogTableRenameTest.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace LogTableRenameTest.WebSite.ViewComponents;

public class UserSidebarViewComponent : ViewComponent
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IMediator _mediator;

    public UserSidebarViewComponent(UserManager<ApplicationUser> userManager, IMediator mediator)
    {
        _userManager = userManager;
        _mediator = mediator;
    }

    public async Task<IViewComponentResult> InvokeAsync(string? currentArea, string? currentController, string? currentAction)
    {
        var area = !string.IsNullOrWhiteSpace(currentArea)
            ? currentArea
            : ViewContext.RouteData.Values["area"]?.ToString();

        var userId = ViewContext.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        
        // Get user information
        string accountName = "کاربر گرامی";
        string accountInitial = "ک";
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
                    accountName = !string.IsNullOrWhiteSpace(user.FullName) ? user.FullName : (user.UserName ?? "کاربر گرامی");
                    accountInitial = accountName.Trim().Length > 0 ? accountName.Trim()[0].ToString() : "ک";
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

        // Get unread tickets count
        var unreadTicketsCount = 0;
        if (!string.IsNullOrWhiteSpace(userId))
        {
            try
            {
                var ticketsCountResult = await _mediator.Send(new GetUnreadTicketsCountForUserQuery(userId), HttpContext.RequestAborted);
                if (ticketsCountResult.IsSuccess)
                {
                    unreadTicketsCount = ticketsCountResult.Value;
                }
            }
            catch
            {
                // Ignore errors in sidebar
            }
        }

        // Get unread notifications count
        var unreadNotificationsCount = 0;
        if (!string.IsNullOrWhiteSpace(userId))
        {
            try
            {
                var notificationsCountResult = await _mediator.Send(new GetUnreadNotificationsCountQuery(userId, "User"), HttpContext.RequestAborted);
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

        var model = new UserSidebarViewModel(
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
            unreadTicketsCount,
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
            return "profile";
        }

        return value.Trim();
    }
}

public record UserSidebarViewModel(
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
    int UnreadTicketsCount = 0,
    int UnreadNotificationsCount = 0);

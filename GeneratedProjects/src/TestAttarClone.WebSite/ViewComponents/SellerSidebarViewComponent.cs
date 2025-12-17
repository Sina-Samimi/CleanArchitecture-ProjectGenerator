using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using TestAttarClone.Application.Queries.Catalog;
using TestAttarClone.Application.Queries.Notifications;
using TestAttarClone.Application.Queries.Orders;
using TestAttarClone.Application.Queries.Sellers;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace TestAttarClone.WebSite.ViewComponents;

public class SellerSidebarViewComponent : ViewComponent
{
    private readonly IMediator _mediator;

    public SellerSidebarViewComponent(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<IViewComponentResult> InvokeAsync(string? currentArea, string? currentController, string? currentAction)
    {
        var area = !string.IsNullOrWhiteSpace(currentArea)
            ? currentArea
            : ViewContext.RouteData.Values["area"]?.ToString();

        var userId = ViewContext.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        
        // Get seller profile information
        string accountName = "فروشنده گرامی";
        string accountInitial = "م";
        string? accountAvatarUrl = null;
        string? accountEmail = null;
        string? accountPhone = null;
        int? profileCompletion = null;

        if (!string.IsNullOrWhiteSpace(userId))
        {
            try
            {
                var profileResult = await _mediator.Send(new GetSellerProfileByUserIdQuery(userId), HttpContext.RequestAborted);
                if (profileResult.IsSuccess && profileResult.Value is not null)
                {
                    var profile = profileResult.Value;
                    accountName = profile.DisplayName;
                    accountInitial = accountName.Trim().Length > 0 ? accountName.Trim()[0].ToString() : "م";
                    accountAvatarUrl = profile.AvatarUrl;
                    accountEmail = profile.ContactEmail;
                    accountPhone = profile.ContactPhone;
                    
                    // Calculate profile completion percentage
                    var totalFields = 8d;
                    var completedFields = 0d;
                    
                    if (!string.IsNullOrWhiteSpace(profile.DisplayName)) completedFields++;
                    if (!string.IsNullOrWhiteSpace(profile.ContactPhone)) completedFields++;
                    if (!string.IsNullOrWhiteSpace(profile.ContactEmail)) completedFields++;
                    if (!string.IsNullOrWhiteSpace(profile.AvatarUrl)) completedFields++;
                    if (!string.IsNullOrWhiteSpace(profile.LicenseNumber)) completedFields++;
                    if (!string.IsNullOrWhiteSpace(profile.ShopAddress)) completedFields++;
                    if (!string.IsNullOrWhiteSpace(profile.WorkingHours)) completedFields++;
                    if (!string.IsNullOrWhiteSpace(profile.Bio)) completedFields++;
                    
                    profileCompletion = (int)Math.Round((completedFields / totalFields) * 100d, MidpointRounding.AwayFromZero);
                    
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
                    if (profileCompletion.HasValue)
                    {
                        ViewContext.ViewData["ProfileCompletionPercent"] = profileCompletion.Value;
                        ViewContext.ViewData["Sidebar:Completion"] = profileCompletion.Value;
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
        profileCompletion = ReadPercent(ViewContext.ViewData["Sidebar:Completion"]) ??
                            ReadPercent(ViewContext.ViewData["ProfileCompletion"]) ??
                            ReadPercent(ViewContext.ViewData["ProfileCompletionPercent"]) ??
                            profileCompletion;
        var greetingSubtitle = ViewContext.ViewData["GreetingSubtitle"] as string;
        var activeTab = ViewContext.ViewData["Sidebar:ActiveTab"] as string;

        // Get new orders count for this seller
        var newOrdersCount = 0;
        if (!string.IsNullOrWhiteSpace(userId))
        {
            try
            {
                var countResult = await _mediator.Send(new GetNewOrdersCountQuery(userId), HttpContext.RequestAborted);
                if (countResult.IsSuccess)
                {
                    newOrdersCount = countResult.Value;
                }
            }
            catch
            {
                // Ignore errors in sidebar
            }
        }

        // Get pending product requests count for this seller
        var pendingProductRequestsCount = 0;
        if (!string.IsNullOrWhiteSpace(userId))
        {
            try
            {
                var requestsCountResult = await _mediator.Send(new GetPendingProductRequestsCountForSellerQuery(userId), HttpContext.RequestAborted);
                if (requestsCountResult.IsSuccess)
                {
                    pendingProductRequestsCount = requestsCountResult.Value;
                }
            }
            catch
            {
                // Ignore errors in sidebar
            }
        }

        // Get unpublished product offers count for this seller
        var unpublishedProductOffersCount = 0;
        if (!string.IsNullOrWhiteSpace(userId))
        {
            try
            {
                var offersCountResult = await _mediator.Send(new GetUnpublishedProductOffersCountForSellerQuery(userId), HttpContext.RequestAborted);
                if (offersCountResult.IsSuccess)
                {
                    unpublishedProductOffersCount = offersCountResult.Value;
                }
            }
            catch
            {
                // Ignore errors in sidebar
            }
        }

        // Get unread notifications count for this seller
        var unreadNotificationsCount = 0;
        if (!string.IsNullOrWhiteSpace(userId))
        {
            try
            {
                var notificationsCountResult = await _mediator.Send(new Application.Queries.Notifications.GetUnreadNotificationsCountQuery(userId, "Seller"), HttpContext.RequestAborted);
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

        var model = new SellerSidebarViewModel(
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
            return "products";
        }

        return value.Trim();
    }
}

public record SellerSidebarViewModel(
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
    int PendingProductRequestsCount = 0,
    int UnpublishedProductOffersCount = 0,
    int UnreadNotificationsCount = 0);

using System;
using Microsoft.AspNetCore.Mvc;

namespace EndPoint.WebSite.ViewComponents;

public class AdminSidebarViewComponent : ViewComponent
{
    public IViewComponentResult Invoke(string? currentArea, string? currentController, string? currentAction)
    {
        var area = !string.IsNullOrWhiteSpace(currentArea)
            ? currentArea
            : ViewContext.RouteData.Values["area"]?.ToString();

        var accountName = ViewContext.ViewData["AccountName"] as string;
        if (string.IsNullOrWhiteSpace(accountName))
        {
            accountName = "مدیر سیستم";
        }

        var accountInitial = ViewContext.ViewData["AccountInitial"] as string;
        if (string.IsNullOrWhiteSpace(accountInitial))
        {
            accountInitial = accountName.Trim().Length > 0
                ? accountName.Trim()[0].ToString()
                : "م";
        }

        var accountAvatarUrl = ViewContext.ViewData["AccountAvatarUrl"] as string;
        var accountEmail = ViewContext.ViewData["AccountEmail"] as string ?? ViewContext.ViewData["Sidebar:Email"] as string;
        var accountPhone = ViewContext.ViewData["AccountPhone"] as string ?? ViewContext.ViewData["Sidebar:Phone"] as string;
        var profileCompletion = ReadPercent(ViewContext.ViewData["Sidebar:Completion"]) ??
                                ReadPercent(ViewContext.ViewData["ProfileCompletion"]) ??
                                ReadPercent(ViewContext.ViewData["ProfileCompletionPercent"]);
        var greetingSubtitle = ViewContext.ViewData["GreetingSubtitle"] as string;
        var activeTab = ViewContext.ViewData["Sidebar:ActiveTab"] as string;

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
            NormalizeTabKey(activeTab));

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
    string? ActiveTab);

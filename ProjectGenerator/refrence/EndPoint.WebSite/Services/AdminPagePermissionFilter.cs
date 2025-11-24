using System;
using System.Linq;
using System.Threading.Tasks;
using Arsis.SharedKernel.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;

namespace EndPoint.WebSite.Services;

/// <summary>
/// فیلتر ساده برای محافظت از Admin Area
/// فقط کاربران با نقش Admin می‌توانند به صفحات Admin دسترسی داشته باشند
/// </summary>
public sealed class AdminPagePermissionFilter : IAsyncAuthorizationFilter
{
    public Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // اگر [AllowAnonymous] داره، اجازه بده
        if (context.Filters.Any(filter => filter is IAllowAnonymousFilter))
        {
            return Task.CompletedTask;
        }

        // چک کردن area
        var routeValues = context.RouteData.Values;
        var area = routeValues.TryGetValue("area", out var areaValue)
            ? Convert.ToString(areaValue) ?? string.Empty
            : string.Empty;

        // فقط برای Admin area چک می‌کنیم
        if (!string.Equals(area, "Admin", StringComparison.OrdinalIgnoreCase))
        {
            return Task.CompletedTask;
        }

        var user = context.HttpContext.User;

        // چک کردن Authentication
        if (user?.Identity?.IsAuthenticated != true)
        {
            context.Result = new ChallengeResult();
            return Task.CompletedTask;
        }

        // چک کردن Role Admin
        if (!user.IsInRole(RoleNames.Admin))
        {
            context.Result = new ForbidResult();
            return Task.CompletedTask;
        }

        // کاربر Admin هست، اجازه دسترسی
        return Task.CompletedTask;
    }
}

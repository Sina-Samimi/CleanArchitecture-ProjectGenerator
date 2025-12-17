using System;
using TestAttarClone.SharedKernel.Authorization;
using Hangfire.Dashboard;

namespace TestAttarClone.WebSite.Services;

/// <summary>
/// محدود کردن دسترسی به داشبورد Hangfire فقط به ادمین‌ها.
/// </summary>
public sealed class HangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));

        var httpContext = context.GetHttpContext();
        if (httpContext == null)
        {
            return false;
        }

        var user = httpContext.User;
        if (user?.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        // فقط کاربران با نقش Admin اجازه مشاهده داشبورد را دارند
        return user.IsInRole(RoleNames.Admin);
    }
}



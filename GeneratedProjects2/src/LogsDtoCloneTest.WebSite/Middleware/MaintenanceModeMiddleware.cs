using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace LogsDtoCloneTest.WebSite.Middleware;

/// <summary>
/// Simple maintenance-mode middleware.
/// When enabled (via configuration), most requests are redirected to the maintenance page,
/// while static assets and (optionally) admin users can still access the site.
/// </summary>
public sealed class MaintenanceModeMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _configuration;

    public MaintenanceModeMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context is null) throw new ArgumentNullException(nameof(context));

        // Read maintenance flag from configuration (appsettings / environment variables)
        // MaintenanceMode:Enabled => bool
        var isMaintenanceEnabled = _configuration.GetValue<bool>("MaintenanceMode:Enabled", false);

        if (!isMaintenanceEnabled)
        {
            await _next(context);
            return;
        }

        var path = context.Request.Path.Value ?? string.Empty;

        // Allow health checks or similar endpoints if needed
        if (path.StartsWith("/health", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        // Allow static files to load so the maintenance page looks correct
        if (path.StartsWith("/css", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/js", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/images", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/img", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/lib", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/font", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/plugins", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        // Allow access to the maintenance page itself and error pages
        if (path.StartsWith("/maintenance", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/error", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        // Allow authenticated admins to bypass maintenance mode
        if (context.User?.Identity?.IsAuthenticated == true &&
            context.User.IsInRole(SharedKernel.Authorization.RoleNames.Admin))
        {
            await _next(context);
            return;
        }

        // Redirect all other requests to maintenance page
        context.Response.Redirect("/Maintenance");
    }
}



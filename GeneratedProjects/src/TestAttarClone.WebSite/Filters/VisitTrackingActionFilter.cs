using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using TestAttarClone.Application.Interfaces;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace TestAttarClone.WebSite.Filters;

public sealed class VisitTrackingActionFilter : IAsyncActionFilter
{
    private readonly IVisitRepository _visitRepository;

    public VisitTrackingActionFilter(IVisitRepository visitRepository)
    {
        _visitRepository = visitRepository;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        await next();

        // Only track GET requests
        if (context.HttpContext.Request.Method != "GET")
        {
            return;
        }

        // Skip tracking for admin area and API endpoints
        var routeData = context.RouteData;
        var area = routeData.Values["area"]?.ToString();
        if (!string.IsNullOrWhiteSpace(area) && area.Equals("Admin", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        // Skip tracking for static files and assets
        var path = context.HttpContext.Request.Path.Value ?? string.Empty;
        if (path.StartsWith("/css", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/js", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/lib", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/images", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/fonts", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/plugins", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/_", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        try
        {
            var viewerIp = GetClientIpAddress(context.HttpContext);
            var visitDate = DateOnly.FromDateTime(DateTime.UtcNow);
            var userAgent = context.HttpContext.Request.Headers["User-Agent"].FirstOrDefault();
            var referrer = context.HttpContext.Request.Headers["Referer"].FirstOrDefault();

            // Track site visit
            await _visitRepository.RegisterSiteVisitAsync(
                viewerIp,
                visitDate,
                userAgent,
                referrer,
                context.HttpContext.RequestAborted);

            // Track page visit if it's a page route
            var controller = routeData.Values["controller"]?.ToString();
            var action = routeData.Values["action"]?.ToString();
            var slug = routeData.Values["slug"]?.ToString();

            if (controller?.Equals("Page", StringComparison.OrdinalIgnoreCase) == true &&
                action?.Equals("Index", StringComparison.OrdinalIgnoreCase) == true &&
                !string.IsNullOrWhiteSpace(slug))
            {
                // Get page ID from slug using repository directly to avoid incrementing view count
                Guid? pageId = null;
                try
                {
                    var pageRepository = context.HttpContext.RequestServices.GetService(typeof(IPageRepository)) as IPageRepository;
                    if (pageRepository is not null)
                    {
                        var page = await pageRepository.GetBySlugAsync(slug, context.HttpContext.RequestAborted);
                        if (page is not null && page.IsPublished)
                        {
                            pageId = page.Id;
                        }
                    }
                }
                catch
                {
                    // Silently fail
                }

                await _visitRepository.RegisterPageVisitAsync(
                    pageId,
                    viewerIp,
                    visitDate,
                    userAgent,
                    referrer,
                    context.HttpContext.RequestAborted);
            }

            // Track product visit if it's a product route
            if (controller?.Equals("Product", StringComparison.OrdinalIgnoreCase) == true &&
                action?.Equals("Details", StringComparison.OrdinalIgnoreCase) == true &&
                !string.IsNullOrWhiteSpace(slug))
            {
                // Get product ID from slug using repository directly to avoid incrementing view count
                Guid? productId = null;
                try
                {
                    var productRepository = context.HttpContext.RequestServices.GetService(typeof(IProductRepository)) as IProductRepository;
                    if (productRepository is not null)
                    {
                        var product = await productRepository.GetBySlugAsync(slug, context.HttpContext.RequestAborted);
                        if (product is not null && product.IsPublished)
                        {
                            productId = product.Id;
                        }
                    }
                }
                catch
                {
                    // Silently fail
                }

                await _visitRepository.RegisterProductVisitAsync(
                    productId,
                    viewerIp,
                    visitDate,
                    userAgent,
                    referrer,
                    context.HttpContext.RequestAborted);
            }
        }
        catch
        {
            // Silently fail to not disrupt the request
        }
    }

    private static string GetClientIpAddress(Microsoft.AspNetCore.Http.HttpContext context)
    {
        // Check for forwarded IP first (for reverse proxy/load balancer)
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(forwardedFor))
        {
            var ips = forwardedFor.Split(',', StringSplitOptions.RemoveEmptyEntries);
            if (ips.Length > 0)
            {
                var ip = ips[0].Trim();
                if (IPAddress.TryParse(ip, out _))
                {
                    return ip;
                }
            }
        }

        var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(realIp) && IPAddress.TryParse(realIp, out _))
        {
            return realIp;
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "0.0.0.0";
    }
}


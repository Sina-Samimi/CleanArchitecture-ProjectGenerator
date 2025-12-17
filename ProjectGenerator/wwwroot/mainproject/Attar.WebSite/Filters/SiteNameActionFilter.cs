using System.Threading.Tasks;
using Attar.Application.Queries.Admin.SiteSettings;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Attar.WebSite.Filters;

public sealed class SiteNameActionFilter : IAsyncActionFilter
{
    private readonly IMediator _mediator;

    public SiteNameActionFilter(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // Get site settings from database
        var siteSettingsResult = await _mediator.Send(new GetSiteSettingsQuery());
        var siteName = "عطاری آنلاین";
        string? logoPath = null;
        string? faviconPath = null;
        string? siteEmail = null;
        string? contactPhone = null;
        string? address = null;
        string? shortDescription = null;
        string? telegramUrl = null;
        string? instagramUrl = null;
        string? whatsAppUrl = null;
        string? linkedInUrl = null;

        if (siteSettingsResult.IsSuccess && siteSettingsResult.Value is not null)
        {
            var settings = siteSettingsResult.Value;
            siteName = settings.SiteTitle;
            logoPath = settings.LogoPath;
            faviconPath = settings.FaviconPath;
            siteEmail = settings.SiteEmail;
            contactPhone = settings.ContactPhone;
            address = settings.Address;
            shortDescription = settings.ShortDescription;
            telegramUrl = settings.TelegramUrl;
            instagramUrl = settings.InstagramUrl;
            whatsAppUrl = settings.WhatsAppUrl;
            linkedInUrl = settings.LinkedInUrl;
        }

        // Set in ViewData for all views
        context.HttpContext.Items["SiteName"] = siteName;
        context.HttpContext.Items["SiteLogoPath"] = logoPath;
        context.HttpContext.Items["SiteFaviconPath"] = faviconPath;
        context.HttpContext.Items["SiteEmail"] = siteEmail;
        context.HttpContext.Items["ContactPhone"] = contactPhone;
        context.HttpContext.Items["Address"] = address;
        context.HttpContext.Items["ShortDescription"] = shortDescription;
        context.HttpContext.Items["TelegramUrl"] = telegramUrl;
        context.HttpContext.Items["InstagramUrl"] = instagramUrl;
        context.HttpContext.Items["WhatsAppUrl"] = whatsAppUrl;
        context.HttpContext.Items["LinkedInUrl"] = linkedInUrl;

        if (context.Controller is Controller controller)
        {
            controller.ViewData["SiteName"] = siteName;
            controller.ViewData["SiteLogoPath"] = logoPath;
            controller.ViewData["SiteFaviconPath"] = faviconPath;
            controller.ViewData["SiteEmail"] = siteEmail;
            controller.ViewData["ContactPhone"] = contactPhone;
            controller.ViewData["Address"] = address;
            controller.ViewData["ShortDescription"] = shortDescription;
            controller.ViewData["TelegramUrl"] = telegramUrl;
            controller.ViewData["InstagramUrl"] = instagramUrl;
            controller.ViewData["WhatsAppUrl"] = whatsAppUrl;
            controller.ViewData["LinkedInUrl"] = linkedInUrl;
        }

        await next();
    }
}


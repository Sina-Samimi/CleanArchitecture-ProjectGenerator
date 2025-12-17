using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LogTableRenameTest.Application.DTOs.Seo;
using LogTableRenameTest.Application.Interfaces;
using LogTableRenameTest.Application.Queries.Seo;
using LogTableRenameTest.Domain.Enums;
using LogTableRenameTest.SharedKernel.Extensions;
using MediatR;
using Microsoft.AspNetCore.Mvc.Filters;

namespace LogTableRenameTest.WebSite.Filters;

public sealed class SeoMetadataActionFilter : IAsyncActionFilter
{
    private readonly ISeoMetadataService _seoService;
    private readonly ISeoTemplateService _templateService;
    private readonly IMediator _mediator;

    public SeoMetadataActionFilter(ISeoMetadataService seoService, ISeoTemplateService templateService, IMediator mediator)
    {
        _seoService = seoService;
        _templateService = templateService;
        _mediator = mediator;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        await next();

        // Skip for admin area
        var area = context.RouteData.Values["area"]?.ToString();
        if (!string.IsNullOrWhiteSpace(area) && area.Equals("Admin", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        // Only process ViewResult
        if (context.Result is not Microsoft.AspNetCore.Mvc.ViewResult viewResult)
        {
            return;
        }

        try
        {
            var pageType = DeterminePageType(context);
            var pageIdentifier = ExtractPageIdentifier(context, pageType);

            var seoMetadata = await _seoService.GetSeoMetadataAsync(pageType, pageIdentifier, context.HttpContext.RequestAborted);
            var faqs = await _seoService.GetPageFaqsAsync(pageType, pageIdentifier, context.HttpContext.RequestAborted);

            if (seoMetadata != null)
            {
                // اگر از Template استفاده می‌کند، متغیرها را استخراج و render کن
                if (seoMetadata.UseTemplate)
                {
                    var variables = ExtractVariables(context, pageType);
                    var dynamicSeo = await _templateService.GenerateDynamicSeo(
                        pageType,
                        pageIdentifier,
                        variables,
                        context.HttpContext.RequestAborted);

                    if (dynamicSeo != null)
                    {
                        seoMetadata = dynamicSeo;

                        // Render Robots template
                        var robotsVariables = new Dictionary<string, object>();
                        robotsVariables["isIndex"] = !context.HttpContext.Request.Query.ContainsKey("page");
                        robotsVariables["hasResults"] = ExtractHasResults(context);
                        
                        var renderedRobots = _templateService.RenderRobotsTemplate(
                            seoMetadata.RobotsTemplate,
                            robotsVariables);

                        if (!string.IsNullOrWhiteSpace(renderedRobots))
                        {
                            seoMetadata = new SeoMetadataDto(
                                seoMetadata.Id,
                                seoMetadata.PageType,
                                seoMetadata.PageIdentifier,
                                seoMetadata.MetaTitle,
                                seoMetadata.MetaDescription,
                                seoMetadata.MetaKeywords,
                                renderedRobots,
                                seoMetadata.CanonicalUrl,
                                seoMetadata.UseTemplate,
                                seoMetadata.TitleTemplate,
                                seoMetadata.DescriptionTemplate,
                                seoMetadata.OgTitleTemplate,
                                seoMetadata.OgDescriptionTemplate,
                                seoMetadata.RobotsTemplate,
                                seoMetadata.OgTitle,
                                seoMetadata.OgDescription,
                                seoMetadata.OgImage,
                                seoMetadata.OgType,
                                seoMetadata.OgUrl,
                                seoMetadata.TwitterCard,
                                seoMetadata.TwitterTitle,
                                seoMetadata.TwitterDescription,
                                seoMetadata.TwitterImage,
                                seoMetadata.SchemaJson,
                                seoMetadata.BreadcrumbsJson,
                                seoMetadata.SitemapPriority,
                                seoMetadata.SitemapChangefreq,
                                seoMetadata.H1Title,
                                seoMetadata.FeaturedImageUrl,
                                seoMetadata.FeaturedImageAlt,
                                seoMetadata.Tags,
                                seoMetadata.Description,
                                seoMetadata.CreateDate,
                                seoMetadata.UpdateDate);
                        }
                    }
                }

                SetViewData(context, seoMetadata, viewResult);

                // دریافت OG Images
                var ogImagesQuery = new GetSeoOgImagesQuery(seoMetadata.Id);
                var ogImagesResult = await _mediator.Send(ogImagesQuery, context.HttpContext.RequestAborted);
                if (ogImagesResult.IsSuccess && ogImagesResult.Value != null && ogImagesResult.Value.Any())
                {
                    viewResult.ViewData["SeoOgImages"] = ogImagesResult.Value;
                    context.HttpContext.Items["SeoOgImages"] = ogImagesResult.Value;
                }
            }

            if (faqs.Faqs.Any())
            {
                context.HttpContext.Items["PageFaqs"] = faqs.Faqs;
                viewResult.ViewData["PageFaqs"] = faqs.Faqs;
            }
        }
        catch
        {
            // Silently fail to not disrupt the request
        }
    }

    private static SeoPageType DeterminePageType(ActionExecutingContext context)
    {
        var controller = context.RouteData.Values["controller"]?.ToString();
        var action = context.RouteData.Values["action"]?.ToString();
        var slug = context.RouteData.Values["slug"]?.ToString();

        return (controller, action, slug) switch
        {
            ("Home", "Index", _) => SeoPageType.Home,
            ("Product", "Index", _) => SeoPageType.ProductList,
            ("Product", "Details", _) => SeoPageType.ProductDetails,
            ("Blog", "Index", _) => SeoPageType.BlogList,
            ("Blog", "Details", _) => SeoPageType.BlogPost,
            ("Page", "Index", _) => SeoPageType.Page,
            ("Home", "Contact", _) => SeoPageType.Contact,
            ("Home", "About", _) => SeoPageType.About,
            _ => SeoPageType.Home
        };
    }

    private static string? ExtractPageIdentifier(ActionExecutingContext context, SeoPageType pageType)
    {
        var slug = context.RouteData.Values["slug"]?.ToString();

        return pageType switch
        {
            SeoPageType.ProductDetails => slug,
            SeoPageType.BlogPost => slug,
            SeoPageType.Page => slug,
            _ => null
        };
    }

    private static Dictionary<string, string> ExtractVariables(ActionExecutingContext context, SeoPageType pageType)
    {
        var variables = new Dictionary<string, string>();

        // متغیرهای عمومی
        var request = context.HttpContext.Request;
        variables["siteName"] = context.HttpContext.Items["SiteName"]?.ToString() ?? "عطاری آنلاین";
        variables["domain"] = $"{request.Scheme}://{request.Host.Value}";
        variables["currentUrl"] = $"{request.Scheme}://{request.Host.Value}{request.Path}{request.QueryString}";

        // تاریخ شمسی
        var persianDate = new System.Globalization.PersianCalendar();
        var now = DateTime.Now;
        variables["year"] = persianDate.GetYear(now).ToString();
        variables["month"] = UserFilterFormatting.GetPersianMonthName(persianDate.GetMonth(now));
        variables["day"] = persianDate.GetDayOfMonth(now).ToString();

        // متغیرهای خاص صفحه - از HttpContext.Items یا RouteData
        switch (pageType)
        {
            case SeoPageType.ProductList:
                variables["category"] = context.HttpContext.Items["category"]?.ToString() 
                    ?? context.RouteData.Values["category"]?.ToString() 
                    ?? context.HttpContext.Request.Query["category"].ToString() 
                    ?? "";
                variables["search"] = context.HttpContext.Items["search"]?.ToString() 
                    ?? context.HttpContext.Request.Query["search"].ToString() 
                    ?? "";
                variables["minPrice"] = context.HttpContext.Items["minPrice"]?.ToString() 
                    ?? context.HttpContext.Request.Query["minPrice"].ToString() 
                    ?? "";
                variables["maxPrice"] = context.HttpContext.Items["maxPrice"]?.ToString() 
                    ?? context.HttpContext.Request.Query["maxPrice"].ToString() 
                    ?? "";
                variables["totalCount"] = context.HttpContext.Items["TotalCount"]?.ToString() 
                    ?? context.HttpContext.Request.Query["totalCount"].ToString() 
                    ?? "";
                break;
            case SeoPageType.BlogList:
                variables["category"] = context.RouteData.Values["category"]?.ToString() ?? "";
                variables["tag"] = context.RouteData.Values["tag"]?.ToString() ?? "";
                break;
        }

        return variables;
    }

    private static bool ExtractHasResults(ActionExecutingContext context)
    {
        // بررسی HttpContext.Items برای TotalCount
        if (context.HttpContext.Items.TryGetValue("TotalCount", out var totalCountObj))
        {
            if (totalCountObj is int count)
            {
                return count > 0;
            }
            if (totalCountObj is string countStr && int.TryParse(countStr, out var parsedCount))
            {
                return parsedCount > 0;
            }
        }

        // بررسی ViewResult.ViewData
        if (context.Result is Microsoft.AspNetCore.Mvc.ViewResult viewResult)
        {
            if (viewResult.ViewData.TryGetValue("TotalCount", out var viewDataCount))
            {
                if (viewDataCount is int count)
                {
                    return count > 0;
                }
            }
        }

        return true; // Default
    }


    private static void SetViewData(ActionExecutingContext context, Application.DTOs.Seo.SeoMetadataDto seoMetadata, Microsoft.AspNetCore.Mvc.ViewResult viewResult)
    {
        var viewData = viewResult.ViewData;
        
        // Merge Strategy: فقط اگر در View/Controller تنظیم نشده باشد، از SEO Metadata استفاده می‌شود
        // این اجازه می‌دهد که View ها یا Controller ها تنظیمات خاص خود را داشته باشند
        
        if (string.IsNullOrWhiteSpace(viewData["Title"]?.ToString()) && !string.IsNullOrWhiteSpace(seoMetadata.MetaTitle))
        {
            viewData["Title"] = seoMetadata.MetaTitle;
        }

        if (string.IsNullOrWhiteSpace(viewData["MetaDescription"]?.ToString()) && !string.IsNullOrWhiteSpace(seoMetadata.MetaDescription))
        {
            viewData["MetaDescription"] = seoMetadata.MetaDescription;
        }

        if (string.IsNullOrWhiteSpace(viewData["MetaKeywords"]?.ToString()) && !string.IsNullOrWhiteSpace(seoMetadata.MetaKeywords))
        {
            viewData["MetaKeywords"] = seoMetadata.MetaKeywords;
        }

        // اگر MetaRobots خالی است، از پیش‌فرض noindex,nofollow استفاده کن
        if (!string.IsNullOrWhiteSpace(seoMetadata.MetaRobots))
        {
            // اگر در View/Controller تنظیم نشده باشد، از SEO Metadata استفاده کن
            if (string.IsNullOrWhiteSpace(viewData["MetaRobots"]?.ToString()))
            {
                viewData["MetaRobots"] = seoMetadata.MetaRobots;
            }
        }
        else if (string.IsNullOrWhiteSpace(viewData["MetaRobots"]?.ToString()))
        {
            // پیش‌فرض: noindex,nofollow
            viewData["MetaRobots"] = "noindex,nofollow";
        }

        if (string.IsNullOrWhiteSpace(viewData["CanonicalUrl"]?.ToString()) && !string.IsNullOrWhiteSpace(seoMetadata.CanonicalUrl))
        {
            viewData["CanonicalUrl"] = seoMetadata.CanonicalUrl;
        }

        if (string.IsNullOrWhiteSpace(viewData["MetaOgTitle"]?.ToString()) && !string.IsNullOrWhiteSpace(seoMetadata.OgTitle))
        {
            viewData["MetaOgTitle"] = seoMetadata.OgTitle;
        }

        if (string.IsNullOrWhiteSpace(viewData["MetaOgDescription"]?.ToString()) && !string.IsNullOrWhiteSpace(seoMetadata.OgDescription))
        {
            viewData["MetaOgDescription"] = seoMetadata.OgDescription;
        }

        if (string.IsNullOrWhiteSpace(viewData["MetaOgImage"]?.ToString()) && !string.IsNullOrWhiteSpace(seoMetadata.OgImage))
        {
            viewData["MetaOgImage"] = seoMetadata.OgImage;
        }

        if (string.IsNullOrWhiteSpace(viewData["MetaOgType"]?.ToString()) && !string.IsNullOrWhiteSpace(seoMetadata.OgType))
        {
            viewData["MetaOgType"] = seoMetadata.OgType;
        }

        if (string.IsNullOrWhiteSpace(viewData["MetaOgUrl"]?.ToString()) && !string.IsNullOrWhiteSpace(seoMetadata.OgUrl))
        {
            viewData["MetaOgUrl"] = seoMetadata.OgUrl;
        }

        if (string.IsNullOrWhiteSpace(viewData["TwitterCard"]?.ToString()) && !string.IsNullOrWhiteSpace(seoMetadata.TwitterCard))
        {
            viewData["TwitterCard"] = seoMetadata.TwitterCard;
        }

        if (string.IsNullOrWhiteSpace(viewData["TwitterTitle"]?.ToString()) && !string.IsNullOrWhiteSpace(seoMetadata.TwitterTitle))
        {
            viewData["TwitterTitle"] = seoMetadata.TwitterTitle;
        }

        if (string.IsNullOrWhiteSpace(viewData["TwitterDescription"]?.ToString()) && !string.IsNullOrWhiteSpace(seoMetadata.TwitterDescription))
        {
            viewData["TwitterDescription"] = seoMetadata.TwitterDescription;
        }

        if (string.IsNullOrWhiteSpace(viewData["TwitterImage"]?.ToString()) && !string.IsNullOrWhiteSpace(seoMetadata.TwitterImage))
        {
            viewData["TwitterImage"] = seoMetadata.TwitterImage;
        }

        // Schema JSON-LD و Breadcrumbs همیشه از SEO Metadata استفاده می‌شود (چون در View/Controller تنظیم نمی‌شود)
        if (!string.IsNullOrWhiteSpace(seoMetadata.SchemaJson))
        {
            viewData["SchemaJson"] = seoMetadata.SchemaJson;
        }

        if (!string.IsNullOrWhiteSpace(seoMetadata.BreadcrumbsJson))
        {
            viewData["BreadcrumbsJson"] = seoMetadata.BreadcrumbsJson;
        }

        // H1 Title - فقط یک عدد در هر صفحه
        if (string.IsNullOrWhiteSpace(viewData["H1Title"]?.ToString()) && !string.IsNullOrWhiteSpace(seoMetadata.H1Title))
        {
            viewData["H1Title"] = seoMetadata.H1Title;
        }

        // Featured Image - تصویر شاخص
        if (string.IsNullOrWhiteSpace(viewData["FeaturedImageUrl"]?.ToString()) && !string.IsNullOrWhiteSpace(seoMetadata.FeaturedImageUrl))
        {
            viewData["FeaturedImageUrl"] = seoMetadata.FeaturedImageUrl;
        }

        if (string.IsNullOrWhiteSpace(viewData["FeaturedImageAlt"]?.ToString()) && !string.IsNullOrWhiteSpace(seoMetadata.FeaturedImageAlt))
        {
            viewData["FeaturedImageAlt"] = seoMetadata.FeaturedImageAlt;
        }

        // Tags
        if (string.IsNullOrWhiteSpace(viewData["Tags"]?.ToString()) && !string.IsNullOrWhiteSpace(seoMetadata.Tags))
        {
            viewData["Tags"] = seoMetadata.Tags;
        }

        // Description (Rich Text)
        if (string.IsNullOrWhiteSpace(viewData["PageDescription"]?.ToString()) && !string.IsNullOrWhiteSpace(seoMetadata.Description))
        {
            viewData["PageDescription"] = seoMetadata.Description;
        }

        // همچنین در HttpContext.Items هم ذخیره کن برای دسترسی در Layout
        context.HttpContext.Items["Title"] = viewData["Title"];
        context.HttpContext.Items["MetaDescription"] = viewData["MetaDescription"];
        context.HttpContext.Items["MetaKeywords"] = viewData["MetaKeywords"];
        context.HttpContext.Items["MetaRobots"] = viewData["MetaRobots"];
        context.HttpContext.Items["CanonicalUrl"] = viewData["CanonicalUrl"];
        context.HttpContext.Items["MetaOgTitle"] = viewData["MetaOgTitle"];
        context.HttpContext.Items["MetaOgDescription"] = viewData["MetaOgDescription"];
        context.HttpContext.Items["MetaOgImage"] = viewData["MetaOgImage"];
        context.HttpContext.Items["MetaOgType"] = viewData["MetaOgType"];
        context.HttpContext.Items["MetaOgUrl"] = viewData["MetaOgUrl"];
        context.HttpContext.Items["TwitterCard"] = viewData["TwitterCard"];
        context.HttpContext.Items["TwitterTitle"] = viewData["TwitterTitle"];
        context.HttpContext.Items["TwitterDescription"] = viewData["TwitterDescription"];
        context.HttpContext.Items["TwitterImage"] = viewData["TwitterImage"];
        context.HttpContext.Items["SchemaJson"] = viewData["SchemaJson"];
        context.HttpContext.Items["BreadcrumbsJson"] = viewData["BreadcrumbsJson"];
    }
}


using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TestAttarClone.Application.Commands.Seo;
using TestAttarClone.Application.DTOs.Seo;
using TestAttarClone.Application.Interfaces;
using TestAttarClone.Application.Queries.Seo;
using TestAttarClone.Domain.Enums;
using TestAttarClone.WebSite.Areas.Admin.Models;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace TestAttarClone.WebSite.Areas.Admin.Controllers;

[Area("Admin")]
public sealed class SeoController : Controller
{
    private const string ContentUploadFolder = "seo/content";
    private const int MaxEditorImageSizeKb = 5 * 1024;
    private const string FeaturedUploadFolder = "seo/featured";
    private const int MaxFeaturedImageSizeKb = 5 * 1024;
    private static readonly HashSet<string> AllowedImageContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/png",
        "image/jpeg",
        "image/webp",
        "image/gif"
    };

    private readonly IMediator _mediator;
    private readonly IFormFileSettingServices _fileSettingServices;

    public SeoController(IMediator mediator, IFormFileSettingServices fileSettingServices)
    {
        _mediator = mediator;
        _fileSettingServices = fileSettingServices;
    }
    private static readonly string[] RobotsOptions =
    {
        "index,follow",
        "index,nofollow",
        "noindex,follow",
        "noindex,nofollow"
    };

    private static readonly string[] OgTypes =
    {
        "website",
        "article",
        "product",
        "book",
        "profile"
    };

    private static readonly string[] TwitterCards =
    {
        "summary",
        "summary_large_image"
    };

    private static readonly string[] SitemapChangefreqOptions =
    {
        "always",
        "hourly",
        "daily",
        "weekly",
        "monthly",
        "yearly",
        "never"
    };

    [HttpGet]
    public async Task<IActionResult> Index(SeoPageType? pageType)
    {
        var cancellationToken = HttpContext.RequestAborted;

        var query = new GetSeoMetadataListQuery(pageType);
        var result = await _mediator.Send(query, cancellationToken);

        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error ?? "خطا در دریافت لیست تنظیمات SEO";
            return View(new SeoMetadataIndexViewModel(Array.Empty<SeoMetadataListItemViewModel>(), 0, pageType));
        }

        var items = result.Value?.Items ?? Array.Empty<SeoMetadataListItemDto>();
        var viewModel = new SeoMetadataIndexViewModel(
            items.Select(item => new SeoMetadataListItemViewModel(
                item.Id,
                item.PageType,
                item.PageIdentifier,
                item.MetaTitle,
                item.MetaDescription,
                item.MetaRobots,
                item.UpdateDate)).ToList(),
            result.Value?.TotalCount ?? 0,
            pageType);

        ViewData["Title"] = "مدیریت SEO";
        ViewData["Subtitle"] = "تنظیمات SEO صفحات";

        return View(viewModel);
    }

    [HttpGet]
    public IActionResult Create(SeoPageType? pageType, string? pageIdentifier)
    {
        var viewModel = new SeoMetadataFormViewModel
        {
            PageType = pageType ?? SeoPageType.Home,
            PageIdentifier = pageIdentifier,
            Selections = BuildFormSelections()
        };

        ViewData["Title"] = "ایجاد تنظیمات SEO";
        ViewData["Subtitle"] = "افزودن تنظیمات SEO جدید";

        return View("Form", viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SeoMetadataFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.Selections = BuildFormSelections();
            ViewData["Title"] = "ایجاد تنظیمات SEO";
            ViewData["Subtitle"] = "افزودن تنظیمات SEO جدید";
            return View("Form", model);
        }

        var cancellationToken = HttpContext.RequestAborted;

        var command = new CreateSeoMetadataCommand(
            model.PageType,
            string.IsNullOrWhiteSpace(model.PageIdentifier) ? null : model.PageIdentifier.Trim(),
            string.IsNullOrWhiteSpace(model.MetaTitle) ? null : model.MetaTitle.Trim(),
            string.IsNullOrWhiteSpace(model.MetaDescription) ? null : model.MetaDescription.Trim(),
            string.IsNullOrWhiteSpace(model.MetaKeywords) ? null : model.MetaKeywords.Trim(),
            string.IsNullOrWhiteSpace(model.MetaRobots) ? null : model.MetaRobots.Trim(),
            string.IsNullOrWhiteSpace(model.CanonicalUrl) ? null : model.CanonicalUrl.Trim(),
            model.UseTemplate,
            string.IsNullOrWhiteSpace(model.TitleTemplate) ? null : model.TitleTemplate.Trim(),
            string.IsNullOrWhiteSpace(model.DescriptionTemplate) ? null : model.DescriptionTemplate.Trim(),
            string.IsNullOrWhiteSpace(model.OgTitleTemplate) ? null : model.OgTitleTemplate.Trim(),
            string.IsNullOrWhiteSpace(model.OgDescriptionTemplate) ? null : model.OgDescriptionTemplate.Trim(),
            string.IsNullOrWhiteSpace(model.RobotsTemplate) ? null : model.RobotsTemplate.Trim(),
            string.IsNullOrWhiteSpace(model.OgTitle) ? null : model.OgTitle.Trim(),
            string.IsNullOrWhiteSpace(model.OgDescription) ? null : model.OgDescription.Trim(),
            string.IsNullOrWhiteSpace(model.OgImage) ? null : model.OgImage.Trim(),
            string.IsNullOrWhiteSpace(model.OgType) ? null : model.OgType.Trim(),
            string.IsNullOrWhiteSpace(model.OgUrl) ? null : model.OgUrl.Trim(),
            string.IsNullOrWhiteSpace(model.TwitterCard) ? null : model.TwitterCard.Trim(),
            string.IsNullOrWhiteSpace(model.TwitterTitle) ? null : model.TwitterTitle.Trim(),
            string.IsNullOrWhiteSpace(model.TwitterDescription) ? null : model.TwitterDescription.Trim(),
            string.IsNullOrWhiteSpace(model.TwitterImage) ? null : model.TwitterImage.Trim(),
            string.IsNullOrWhiteSpace(model.SchemaJson) ? null : model.SchemaJson.Trim(),
            string.IsNullOrWhiteSpace(model.BreadcrumbsJson) ? null : model.BreadcrumbsJson.Trim(),
            model.SitemapPriority,
            string.IsNullOrWhiteSpace(model.SitemapChangefreq) ? null : model.SitemapChangefreq.Trim(),
            string.IsNullOrWhiteSpace(model.H1Title) ? null : model.H1Title.Trim(),
            string.IsNullOrWhiteSpace(model.FeaturedImageUrl) ? null : model.FeaturedImageUrl.Trim(),
            string.IsNullOrWhiteSpace(model.FeaturedImageAlt) ? null : model.FeaturedImageAlt.Trim(),
            string.IsNullOrWhiteSpace(model.Tags) ? null : model.Tags.Trim(),
            string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim());

        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "خطا در ایجاد تنظیمات SEO");
            model.Selections = BuildFormSelections();
            ViewData["Title"] = "ایجاد تنظیمات SEO";
            ViewData["Subtitle"] = "افزودن تنظیمات SEO جدید";
            return View("Form", model);
        }

        TempData["Success"] = "تنظیمات SEO با موفقیت ایجاد شد";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var cancellationToken = HttpContext.RequestAborted;

        var query = new GetSeoMetadataByIdQuery(id);
        var result = await _mediator.Send(query, cancellationToken);
        
        if (!result.IsSuccess || result.Value is null)
        {
            TempData["Error"] = "تنظیمات SEO مورد نظر یافت نشد";
            return RedirectToAction(nameof(Index));
        }

        var seoMetadata = result.Value;

        var viewModel = new SeoMetadataFormViewModel
        {
            Id = seoMetadata.Id,
            PageType = seoMetadata.PageType,
            PageIdentifier = seoMetadata.PageIdentifier,
            UseTemplate = seoMetadata.UseTemplate,
            TitleTemplate = seoMetadata.TitleTemplate,
            DescriptionTemplate = seoMetadata.DescriptionTemplate,
            OgTitleTemplate = seoMetadata.OgTitleTemplate,
            OgDescriptionTemplate = seoMetadata.OgDescriptionTemplate,
            RobotsTemplate = seoMetadata.RobotsTemplate,
            MetaTitle = seoMetadata.MetaTitle,
            MetaDescription = seoMetadata.MetaDescription,
            MetaKeywords = seoMetadata.MetaKeywords,
            MetaRobots = seoMetadata.MetaRobots,
            CanonicalUrl = seoMetadata.CanonicalUrl,
            OgTitle = seoMetadata.OgTitle,
            OgDescription = seoMetadata.OgDescription,
            OgImage = seoMetadata.OgImage,
            OgType = seoMetadata.OgType,
            OgUrl = seoMetadata.OgUrl,
            TwitterCard = seoMetadata.TwitterCard,
            TwitterTitle = seoMetadata.TwitterTitle,
            TwitterDescription = seoMetadata.TwitterDescription,
            TwitterImage = seoMetadata.TwitterImage,
            SchemaJson = seoMetadata.SchemaJson,
            BreadcrumbsJson = seoMetadata.BreadcrumbsJson,
            SitemapPriority = seoMetadata.SitemapPriority,
            SitemapChangefreq = seoMetadata.SitemapChangefreq,
            H1Title = seoMetadata.H1Title,
            FeaturedImageUrl = seoMetadata.FeaturedImageUrl,
            FeaturedImageAlt = seoMetadata.FeaturedImageAlt,
            Tags = seoMetadata.Tags,
            Description = seoMetadata.Description,
            Selections = BuildFormSelections()
        };

        ViewData["Title"] = "ویرایش تنظیمات SEO";
        ViewData["Subtitle"] = $"ویرایش تنظیمات SEO: {GetPageTypeName(seoMetadata.PageType)}";

        return View("Form", viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(SeoMetadataFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.Selections = BuildFormSelections();
            ViewData["Title"] = "ویرایش تنظیمات SEO";
            ViewData["Subtitle"] = "ویرایش تنظیمات SEO";
            return View("Form", model);
        }

        if (!model.Id.HasValue)
        {
            ModelState.AddModelError(string.Empty, "شناسه تنظیمات SEO نامعتبر است");
            model.Selections = BuildFormSelections();
            ViewData["Title"] = "ویرایش تنظیمات SEO";
            ViewData["Subtitle"] = "ویرایش تنظیمات SEO";
            return View("Form", model);
        }

        var cancellationToken = HttpContext.RequestAborted;

        var command = new UpdateSeoMetadataCommand(
            model.Id.Value,
            string.IsNullOrWhiteSpace(model.MetaTitle) ? null : model.MetaTitle.Trim(),
            string.IsNullOrWhiteSpace(model.MetaDescription) ? null : model.MetaDescription.Trim(),
            string.IsNullOrWhiteSpace(model.MetaKeywords) ? null : model.MetaKeywords.Trim(),
            string.IsNullOrWhiteSpace(model.MetaRobots) ? null : model.MetaRobots.Trim(),
            string.IsNullOrWhiteSpace(model.CanonicalUrl) ? null : model.CanonicalUrl.Trim(),
            model.UseTemplate,
            string.IsNullOrWhiteSpace(model.TitleTemplate) ? null : model.TitleTemplate.Trim(),
            string.IsNullOrWhiteSpace(model.DescriptionTemplate) ? null : model.DescriptionTemplate.Trim(),
            string.IsNullOrWhiteSpace(model.OgTitleTemplate) ? null : model.OgTitleTemplate.Trim(),
            string.IsNullOrWhiteSpace(model.OgDescriptionTemplate) ? null : model.OgDescriptionTemplate.Trim(),
            string.IsNullOrWhiteSpace(model.RobotsTemplate) ? null : model.RobotsTemplate.Trim(),
            string.IsNullOrWhiteSpace(model.OgTitle) ? null : model.OgTitle.Trim(),
            string.IsNullOrWhiteSpace(model.OgDescription) ? null : model.OgDescription.Trim(),
            string.IsNullOrWhiteSpace(model.OgImage) ? null : model.OgImage.Trim(),
            string.IsNullOrWhiteSpace(model.OgType) ? null : model.OgType.Trim(),
            string.IsNullOrWhiteSpace(model.OgUrl) ? null : model.OgUrl.Trim(),
            string.IsNullOrWhiteSpace(model.TwitterCard) ? null : model.TwitterCard.Trim(),
            string.IsNullOrWhiteSpace(model.TwitterTitle) ? null : model.TwitterTitle.Trim(),
            string.IsNullOrWhiteSpace(model.TwitterDescription) ? null : model.TwitterDescription.Trim(),
            string.IsNullOrWhiteSpace(model.TwitterImage) ? null : model.TwitterImage.Trim(),
            string.IsNullOrWhiteSpace(model.SchemaJson) ? null : model.SchemaJson.Trim(),
            string.IsNullOrWhiteSpace(model.BreadcrumbsJson) ? null : model.BreadcrumbsJson.Trim(),
            model.SitemapPriority,
            string.IsNullOrWhiteSpace(model.SitemapChangefreq) ? null : model.SitemapChangefreq.Trim(),
            string.IsNullOrWhiteSpace(model.H1Title) ? null : model.H1Title.Trim(),
            string.IsNullOrWhiteSpace(model.FeaturedImageUrl) ? null : model.FeaturedImageUrl.Trim(),
            string.IsNullOrWhiteSpace(model.FeaturedImageAlt) ? null : model.FeaturedImageAlt.Trim(),
            string.IsNullOrWhiteSpace(model.Tags) ? null : model.Tags.Trim(),
            string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim());

        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "خطا در به‌روزرسانی تنظیمات SEO");
            model.Selections = BuildFormSelections();
            ViewData["Title"] = "ویرایش تنظیمات SEO";
            ViewData["Subtitle"] = "ویرایش تنظیمات SEO";
            return View("Form", model);
        }

        TempData["Success"] = "تنظیمات SEO با موفقیت به‌روزرسانی شد";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> OgImages(Guid seoMetadataId)
    {
        var cancellationToken = HttpContext.RequestAborted;

        var query = new GetSeoOgImagesQuery(seoMetadataId);
        var result = await _mediator.Send(query, cancellationToken);

        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error ?? "خطا در دریافت تصاویر OG";
            return RedirectToAction(nameof(Index));
        }

        ViewData["Title"] = "مدیریت تصاویر Open Graph";
        ViewData["Subtitle"] = "تصاویر OG";
        ViewData["SeoMetadataId"] = seoMetadataId;

        return View(result.Value ?? Array.Empty<Application.DTOs.Seo.SeoOgImageDto>());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateOgImage(Guid seoMetadataId, string imageUrl, int displayOrder, int? width, int? height, string? imageType, string? alt)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            TempData["Error"] = "URL تصویر الزامی است";
            return RedirectToAction(nameof(OgImages), new { seoMetadataId });
        }

        var cancellationToken = HttpContext.RequestAborted;

        var command = new CreateSeoOgImageCommand(
            seoMetadataId,
            imageUrl.Trim(),
            displayOrder,
            width,
            height,
            string.IsNullOrWhiteSpace(imageType) ? null : imageType.Trim(),
            string.IsNullOrWhiteSpace(alt) ? null : alt.Trim());

        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error ?? "خطا در ایجاد تصویر OG";
        }
        else
        {
            TempData["Success"] = "تصویر OG با موفقیت ایجاد شد";
        }

        return RedirectToAction(nameof(OgImages), new { seoMetadataId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteOgImage(Guid id, Guid seoMetadataId)
    {
        var cancellationToken = HttpContext.RequestAborted;

        var command = new DeleteSeoOgImageCommand(id);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error ?? "خطا در حذف تصویر OG";
        }
        else
        {
            TempData["Success"] = "تصویر OG با موفقیت حذف شد";
        }

        return RedirectToAction(nameof(OgImages), new { seoMetadataId });
    }

    [HttpGet]
    public async Task<IActionResult> Faqs(SeoPageType pageType, string? pageIdentifier)
    {
        var cancellationToken = HttpContext.RequestAborted;

        var query = new GetPageFaqsQuery(pageType, pageIdentifier);
        var result = await _mediator.Send(query, cancellationToken);

        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error ?? "خطا در دریافت سوالات متداول";
            return View(new PageFaqListViewModel(pageType, pageIdentifier, Array.Empty<PageFaqDto>()));
        }

        var viewModel = new PageFaqListViewModel(
            pageType,
            pageIdentifier,
            result.Value?.Faqs ?? Array.Empty<PageFaqDto>());

        ViewData["Title"] = "مدیریت سوالات متداول";
        ViewData["Subtitle"] = $"سوالات متداول: {GetPageTypeName(pageType)}";

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateFaq(PageFaqFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return RedirectToAction(nameof(Faqs), new { pageType = model.PageType, pageIdentifier = model.PageIdentifier });
        }

        var cancellationToken = HttpContext.RequestAborted;

        var command = new CreatePageFaqCommand(
            model.PageType,
            model.Question,
            model.Answer,
            model.DisplayOrder,
            string.IsNullOrWhiteSpace(model.PageIdentifier) ? null : model.PageIdentifier.Trim());

        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error ?? "خطا در ایجاد سوال متداول";
        }
        else
        {
            TempData["Success"] = "سوال متداول با موفقیت ایجاد شد";
        }

        return RedirectToAction(nameof(Faqs), new { pageType = model.PageType, pageIdentifier = model.PageIdentifier });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteFaq(Guid id, SeoPageType pageType, string? pageIdentifier)
    {
        var cancellationToken = HttpContext.RequestAborted;

        var command = new DeletePageFaqCommand(id);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error ?? "خطا در حذف سوال متداول";
        }
        else
        {
            TempData["Success"] = "سوال متداول با موفقیت حذف شد";
        }

        return RedirectToAction(nameof(Faqs), new { pageType, pageIdentifier });
    }

    private SeoFormSelections BuildFormSelections()
    {
        return new SeoFormSelections
        {
            PageTypes = Enum.GetValues<SeoPageType>()
                .Select(pt => new SelectListItem
                {
                    Value = ((int)pt).ToString(),
                    Text = GetPageTypeName(pt)
                })
                .ToList(),
            RobotsOptions = RobotsOptions
                .Select(r => new SelectListItem { Value = r, Text = r })
                .ToList(),
            OgTypes = OgTypes
                .Select(ot => new SelectListItem { Value = ot, Text = ot })
                .ToList(),
            TwitterCards = TwitterCards
                .Select(tc => new SelectListItem { Value = tc, Text = tc })
                .ToList(),
            SitemapChangefreqOptions = SitemapChangefreqOptions
                .Select(sc => new SelectListItem { Value = sc, Text = sc })
                .ToList()
        };
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult UploadContentImage(IFormFile? file)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { error = "فایلی برای آپلود ارسال نشده است." });
        }

        if (!_fileSettingServices.IsFileSizeValid(file, MaxEditorImageSizeKb))
        {
            return BadRequest(new { error = "حجم تصویر باید کمتر از ۵ مگابایت باشد." });
        }

        var contentType = file.ContentType ?? string.Empty;
        if (!AllowedImageContentTypes.Contains(contentType))
        {
            return BadRequest(new { error = "فرمت تصویر پشتیبانی نمی‌شود." });
        }

        var response = _fileSettingServices.UploadImage(ContentUploadFolder, file, Guid.NewGuid().ToString("N"));
        if (!response.Success || string.IsNullOrWhiteSpace(response.Data))
        {
            return BadRequest(new { error = response.Messages.FirstOrDefault()?.message ?? "امکان ذخیره‌سازی تصویر وجود ندارد." });
        }

        var normalizedPath = response.Data.Replace("\\", "/");
        if (!normalizedPath.StartsWith('/'))
        {
            normalizedPath = "/" + normalizedPath;
        }

        return Json(new { url = normalizedPath });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult UploadFeaturedImage(IFormFile? file)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { error = "فایلی برای آپلود ارسال نشده است." });
        }

        if (!_fileSettingServices.IsFileSizeValid(file, MaxFeaturedImageSizeKb))
        {
            return BadRequest(new { error = "حجم تصویر باید کمتر از ۵ مگابایت باشد." });
        }

        var contentType = file.ContentType ?? string.Empty;
        if (!AllowedImageContentTypes.Contains(contentType))
        {
            return BadRequest(new { error = "فرمت تصویر پشتیبانی نمی‌شود." });
        }

        var response = _fileSettingServices.UploadImage(FeaturedUploadFolder, file, Guid.NewGuid().ToString("N"));
        if (!response.Success || string.IsNullOrWhiteSpace(response.Data))
        {
            return BadRequest(new { error = response.Messages.FirstOrDefault()?.message ?? "امکان ذخیره‌سازی تصویر وجود ندارد." });
        }

        var normalizedPath = response.Data.Replace("\\", "/");
        if (!normalizedPath.StartsWith('/'))
        {
            normalizedPath = "/" + normalizedPath;
        }

        return Json(new { url = normalizedPath });
    }

    private static string GetPageTypeName(SeoPageType pageType)
    {
        return pageType switch
        {
            SeoPageType.Home => "صفحه اصلی",
            SeoPageType.ProductList => "لیست محصولات",
            SeoPageType.ProductDetails => "جزییات محصول",
            SeoPageType.BlogList => "لیست وبلاگ",
            SeoPageType.BlogPost => "پست وبلاگ",
            SeoPageType.Page => "صفحه داینامیک",
            SeoPageType.Contact => "تماس با ما",
            SeoPageType.About => "درباره ما",
            SeoPageType.Category => "دسته‌بندی محصولات",
            SeoPageType.BlogCategory => "دسته‌بندی وبلاگ",
            SeoPageType.Search => "صفحه جستجو",
            SeoPageType.Cart => "سبد خرید",
            SeoPageType.Checkout => "تسویه حساب",
            _ => pageType.ToString()
        };
    }
}


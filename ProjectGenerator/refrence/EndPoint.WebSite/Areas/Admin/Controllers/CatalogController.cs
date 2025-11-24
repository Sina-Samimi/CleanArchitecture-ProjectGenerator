using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Arsis.Application.Commands.Catalog;
using Arsis.Application.DTOs.Catalog;
using Arsis.Application.DTOs.Teachers;
using Arsis.Application.Interfaces;
using Arsis.Application.Queries.Catalog;
using Arsis.Application.Queries.Teachers;
using Arsis.Domain.Enums;
using EndPoint.WebSite.Areas.Admin.Models;
using Arsis.SharedKernel.BaseTypes;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EndPoint.WebSite.Areas.Admin.Controllers;

[Area("Admin")]
public sealed class CatalogController : Controller
{
    private const string FeaturedUploadFolder = "products/featured";
    private const string GalleryUploadFolder = "products/gallery";
    private const string ContentUploadFolder = "products/content";
    private const int MaxImageSizeKb = 5 * 1024;
    private const string DigitalUploadFolder = "files/products";
    private const int MaxDigitalFileSizeKb = 200 * 1024;

    private static readonly int[] PageSizeOptions = { 12, 24, 36, 48 };
    private static readonly string[] RobotsOptionValues =
    {
        "index,follow",
        "index,nofollow",
        "noindex,follow",
        "noindex,nofollow"
    };

    private static readonly HashSet<string> AllowedImageContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/png",
        "image/jpeg",
        "image/webp",
        "image/gif"
    };

    private static readonly HashSet<string> AllowedDigitalExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".zip",
        ".rar",
        ".7z",
        ".pdf",
        ".doc",
        ".docx",
        ".ppt",
        ".pptx",
        ".xls",
        ".xlsx",
        ".txt",
        ".mp3",
        ".mp4"
    };

    private readonly IMediator _mediator;
    private readonly IFormFileSettingServices _fileSettingServices;

    public CatalogController(IMediator mediator, IFormFileSettingServices fileSettingServices)
    {
        _mediator = mediator;
        _fileSettingServices = fileSettingServices;
    }

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] ProductIndexRequest? request)
    {
        request ??= new ProductIndexRequest();
        var cancellationToken = HttpContext.RequestAborted;

        var viewModel = await BuildProductIndexViewModelAsync(request, cancellationToken);

        ConfigureProductIndexViewData();

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var cancellationToken = HttpContext.RequestAborted;
        var model = await BuildProductFormViewModelAsync(new ProductFormViewModel(), cancellationToken);

        ConfigureProductFormViewData(isEdit: false);

        return View("Form", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProductFormViewModel model)
    {
        var cancellationToken = HttpContext.RequestAborted;

        NormalizeProductFormModel(model);
        EnsureGalleryInitialized(model);

        await BuildProductFormViewModelAsync(model, cancellationToken);

        var tags = ParseTags(model.Tags);
        model.TagItems = tags;
        model.Tags = string.Join(", ", tags);
        ModelState.Remove(nameof(ProductFormViewModel.Tags));

        if (model.CategoryId is null)
        {
            ModelState.AddModelError(nameof(ProductFormViewModel.CategoryId), "دسته‌بندی محصول را انتخاب کنید.");
        }

        var hasDigitalFile = model.DigitalDownloadFile is { Length: > 0 };
        if (model.Type == ProductType.Digital)
        {
            if (!hasDigitalFile && string.IsNullOrWhiteSpace(model.DigitalDownloadPath))
            {
                ModelState.AddModelError(
                    nameof(ProductFormViewModel.DigitalDownloadPath),
                    "برای محصولات دانلودی وارد کردن لینک یا آپلود فایل الزامی است.");
            }

            if (hasDigitalFile)
            {
                ValidateDigitalFile(model.DigitalDownloadFile, nameof(ProductFormViewModel.DigitalDownloadFile));
            }
        }
        else
        {
            model.DigitalDownloadPath = null;
        }

        if (!model.TrackInventory)
        {
            model.StockQuantity = 0;
        }

        ValidateFeaturedImage(model.FeaturedImage, nameof(ProductFormViewModel.FeaturedImage));
        ValidateGalleryFiles(model.Gallery);

        if (!TryResolvePublishState(model, out var isPublished, out var publishedAt))
        {
            ConfigureProductFormViewData(isEdit: false);
            return View("Form", model);
        }

        var robots = NormalizeRobots(model.Robots);
        if (!IsRobotsOptionValid(robots))
        {
            ModelState.AddModelError(nameof(ProductFormViewModel.Robots), "مقدار Robots معتبر نیست.");
        }

        if (!ModelState.IsValid)
        {
            ConfigureProductFormViewData(isEdit: false);
            return View("Form", model);
        }

        if (model.Type == ProductType.Digital && model.DigitalDownloadFile is { Length: > 0 })
        {
            var digitalPath = await SaveDigitalFileAsync(
                model.DigitalDownloadFile,
                nameof(ProductFormViewModel.DigitalDownloadFile));

            if (digitalPath is null)
            {
                ConfigureProductFormViewData(isEdit: false);
                return View("Form", model);
            }

            model.DigitalDownloadPath = digitalPath;
        }

        var featuredPath = await SaveFeaturedImageAsync(model.FeaturedImage, nameof(ProductFormViewModel.FeaturedImage));
        if (featuredPath is null && model.FeaturedImage is { Length: > 0 })
        {
            ConfigureProductFormViewData(isEdit: false);
            return View("Form", model);
        }

        if (!string.IsNullOrWhiteSpace(featuredPath))
        {
            model.FeaturedImagePath = featuredPath;
        }

        var galleryItems = await SaveGalleryImagesAsync(model.Gallery, cancellationToken);
        if (galleryItems is null)
        {
            ConfigureProductFormViewData(isEdit: false);
            return View("Form", model);
        }

        model.Robots = robots;

        var command = new CreateProductCommand(
            model.Name,
            model.Summary ?? string.Empty,
            model.Description,
            model.Type,
            model.Price,
            model.CompareAtPrice,
            model.TrackInventory,
            model.StockQuantity,
            model.CategoryId!.Value,
            isPublished,
            publishedAt,
            model.SeoTitle,
            model.SeoDescription ?? string.Empty,
            model.SeoKeywords ?? string.Empty,
            model.SeoSlug,
            model.Robots,
            string.IsNullOrWhiteSpace(model.FeaturedImagePath) ? null : model.FeaturedImagePath,
            model.Type == ProductType.Digital ? model.DigitalDownloadPath : null,
            tags,
            galleryItems.Select(item => new CreateProductCommand.ProductGalleryItem(item.Path, item.Order)).ToArray(),
            model.TeacherId);

        var result = await _mediator.Send(command, cancellationToken);
        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "خطا در ثبت محصول.");
            ConfigureProductFormViewData(isEdit: false);
            return View("Form", model);
        }

        TempData["Success"] = "محصول جدید با موفقیت ثبت شد.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var cancellationToken = HttpContext.RequestAborted;
        var result = await _mediator.Send(new GetProductDetailQuery(id), cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return NotFound();
        }

        var dto = result.Value;
        var tags = ParseTags(dto.TagList);
        var gallery = dto.Gallery?
            .OrderBy(image => image.DisplayOrder)
            .Select(image => new ProductGalleryItemFormModel
            {
                Id = image.Id,
                Path = image.ImagePath,
                Order = image.DisplayOrder
            })
            .ToList() ?? new List<ProductGalleryItemFormModel>();

        var model = new ProductFormViewModel
        {
            Id = dto.Id,
            Name = dto.Name,
            Summary = string.IsNullOrWhiteSpace(dto.Summary) ? null : dto.Summary,
            Description = dto.Description,
            Type = dto.Type,
            Price = dto.Price,
            CompareAtPrice = dto.CompareAtPrice,
            TrackInventory = dto.TrackInventory,
            StockQuantity = dto.StockQuantity,
            CategoryId = dto.CategoryId,
            PublishStatus = DeterminePublishStatus(dto.IsPublished, dto.PublishedAt),
            PublishedAt = dto.PublishedAt,
            SeoTitle = dto.SeoTitle,
            SeoDescription = string.IsNullOrWhiteSpace(dto.SeoDescription) ? null : dto.SeoDescription,
            SeoKeywords = string.IsNullOrWhiteSpace(dto.SeoKeywords) ? null : dto.SeoKeywords,
            SeoSlug = dto.SeoSlug,
            Robots = dto.Robots,
            Tags = string.Join(", ", tags),
            TagItems = tags,
            FeaturedImagePath = dto.FeaturedImagePath,
            DigitalDownloadPath = dto.DigitalDownloadPath,
            Gallery = gallery,
            TeacherId = dto.TeacherId
        };

        await BuildProductFormViewModelAsync(model, cancellationToken);

        ConfigureProductFormViewData(isEdit: true);

        return View("Form", model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id)
    {
        var cancellationToken = HttpContext.RequestAborted;

        var productResult = await _mediator.Send(new GetProductDetailQuery(id), cancellationToken);
        if (!productResult.IsSuccess || productResult.Value is null)
        {
            return NotFound();
        }

        var product = productResult.Value;

        var salesResult = await _mediator.Send(new GetProductSalesSummaryQuery(id), cancellationToken);
        var salesViewModel = salesResult.IsSuccess && salesResult.Value is not null
            ? MapSalesToViewModel(salesResult.Value)
            : ProductSalesSummaryViewModel.Empty;

        var gallery = product.Gallery
            .OrderBy(image => image.DisplayOrder)
            .ThenBy(image => image.Id)
            .Select(image => new ProductGalleryItemViewModel(image.Id, image.ImagePath, image.DisplayOrder))
            .ToArray();

        var viewModel = new ProductDetailViewModel
        {
            Id = product.Id,
            Name = product.Name,
            Summary = product.Summary,
            Description = product.Description,
            Type = product.Type,
            CategoryName = product.CategoryName,
            Price = product.Price,
            CompareAtPrice = product.CompareAtPrice,
            TrackInventory = product.TrackInventory,
            StockQuantity = product.StockQuantity,
            IsPublished = product.IsPublished,
            PublishedAt = product.PublishedAt,
            SeoTitle = product.SeoTitle,
            SeoDescription = product.SeoDescription,
            SeoKeywords = product.SeoKeywords,
            SeoSlug = product.SeoSlug,
            Robots = product.Robots,
            TagList = product.TagList,
            FeaturedImagePath = product.FeaturedImagePath,
            DigitalDownloadPath = product.DigitalDownloadPath,
            Gallery = gallery,
            Sales = salesViewModel
        };

        ViewData["Title"] = $"جزئیات محصول - {product.Name}";

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, ProductFormViewModel model)
    {
        var cancellationToken = HttpContext.RequestAborted;
        model.Id = id;

        NormalizeProductFormModel(model);
        EnsureGalleryInitialized(model);

        await BuildProductFormViewModelAsync(model, cancellationToken);

        var tags = ParseTags(model.Tags);
        model.TagItems = tags;
        model.Tags = string.Join(", ", tags);
        ModelState.Remove(nameof(ProductFormViewModel.Tags));

        if (model.Id is null || model.Id == Guid.Empty)
        {
            ModelState.AddModelError(string.Empty, "شناسه محصول معتبر نیست.");
        }

        if (model.CategoryId is null)
        {
            ModelState.AddModelError(nameof(ProductFormViewModel.CategoryId), "دسته‌بندی محصول را انتخاب کنید.");
        }

        var hasDigitalFile = model.DigitalDownloadFile is { Length: > 0 };
        if (model.Type == ProductType.Digital)
        {
            if (!hasDigitalFile && string.IsNullOrWhiteSpace(model.DigitalDownloadPath))
            {
                ModelState.AddModelError(
                    nameof(ProductFormViewModel.DigitalDownloadPath),
                    "برای محصولات دانلودی وارد کردن لینک یا آپلود فایل الزامی است.");
            }

            if (hasDigitalFile)
            {
                ValidateDigitalFile(model.DigitalDownloadFile, nameof(ProductFormViewModel.DigitalDownloadFile));
            }
        }
        else
        {
            model.DigitalDownloadPath = null;
        }

        if (!model.TrackInventory)
        {
            model.StockQuantity = 0;
        }

        ValidateFeaturedImage(model.FeaturedImage, nameof(ProductFormViewModel.FeaturedImage));
        ValidateGalleryFiles(model.Gallery);

        if (!TryResolvePublishState(model, out var isPublished, out var publishedAt))
        {
            ConfigureProductFormViewData(isEdit: true);
            return View("Form", model);
        }

        var robots = NormalizeRobots(model.Robots);
        if (!IsRobotsOptionValid(robots))
        {
            ModelState.AddModelError(nameof(ProductFormViewModel.Robots), "مقدار Robots معتبر نیست.");
        }

        if (!ModelState.IsValid)
        {
            ConfigureProductFormViewData(isEdit: true);
            return View("Form", model);
        }

        if (model.Type == ProductType.Digital && model.DigitalDownloadFile is { Length: > 0 })
        {
            var digitalPath = await SaveDigitalFileAsync(
                model.DigitalDownloadFile,
                nameof(ProductFormViewModel.DigitalDownloadFile));

            if (digitalPath is null)
            {
                ConfigureProductFormViewData(isEdit: true);
                return View("Form", model);
            }

            model.DigitalDownloadPath = digitalPath;
        }

        string? featuredPath = model.FeaturedImagePath;
        if (model.RemoveFeaturedImage)
        {
            featuredPath = null;
        }

        if (model.FeaturedImage is { Length: > 0 })
        {
            featuredPath = await SaveFeaturedImageAsync(model.FeaturedImage, nameof(ProductFormViewModel.FeaturedImage));
            if (featuredPath is null)
            {
                ConfigureProductFormViewData(isEdit: true);
                return View("Form", model);
            }
        }

        var galleryItems = await SaveGalleryImagesAsync(model.Gallery, cancellationToken);
        if (galleryItems is null)
        {
            ConfigureProductFormViewData(isEdit: true);
            return View("Form", model);
        }

        model.Robots = robots;

        var command = new UpdateProductCommand(
            model.Id!.Value,
            model.Name,
            model.Summary ?? string.Empty,
            model.Description,
            model.Type,
            model.Price,
            model.CompareAtPrice,
            model.TrackInventory,
            model.StockQuantity,
            model.CategoryId!.Value,
            isPublished,
            publishedAt,
            model.SeoTitle,
            model.SeoDescription ?? string.Empty,
            model.SeoKeywords ?? string.Empty,
            model.SeoSlug,
            model.Robots,
            string.IsNullOrWhiteSpace(featuredPath) ? null : featuredPath,
            model.Type == ProductType.Digital ? model.DigitalDownloadPath : null,
            tags,
            galleryItems.Select(item => new UpdateProductCommand.ProductGalleryItem(item.Path, item.Order)).ToArray(),
            model.TeacherId);

        var result = await _mediator.Send(command, cancellationToken);
        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "خطا در بروزرسانی محصول.");
            ConfigureProductFormViewData(isEdit: true);
            return View("Form", model);
        }

        TempData["Success"] = "تغییرات محصول با موفقیت ذخیره شد.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> ProductDetail(Guid id)
    {
        var cancellationToken = HttpContext.RequestAborted;
        var result = await _mediator.Send(new GetProductDetailQuery(id), cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return NotFound(new { message = result.Error ?? "محصول مورد نظر یافت نشد." });
        }

        var dto = result.Value;
        var response = new
        {
            id = dto.Id,
            name = dto.Name,
            summary = dto.Summary,
            description = dto.Description,
            type = dto.Type.ToString(),
            price = dto.Price,
            compareAtPrice = dto.CompareAtPrice,
            trackInventory = dto.TrackInventory,
            stockQuantity = dto.StockQuantity,
            isPublished = dto.IsPublished,
            publishedAt = dto.PublishedAt?.ToString("o"),
            categoryId = dto.CategoryId,
            seoTitle = dto.SeoTitle,
            seoDescription = dto.SeoDescription,
            seoKeywords = dto.SeoKeywords,
            seoSlug = dto.SeoSlug,
            robots = dto.Robots,
            tagList = dto.TagList,
            featuredImagePath = dto.FeaturedImagePath,
            digitalDownloadPath = dto.DigitalDownloadPath,
            gallery = dto.Gallery?.Select(image => new { path = image.ImagePath, order = image.DisplayOrder }).ToArray()
        };

        return Json(response);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult UploadContentImage(IFormFile? file)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { error = "فایلی برای آپلود ارسال نشده است." });
        }

        if (!_fileSettingServices.IsFileSizeValid(file, MaxImageSizeKb))
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

        var normalizedPath = response.Data.Replace("\\", "/", StringComparison.Ordinal);
        if (!normalizedPath.StartsWith('/'))
        {
            normalizedPath = "/" + normalizedPath;
        }

        return Json(new { url = normalizedPath });
    }

    private static ProductSalesSummaryViewModel MapSalesToViewModel(ProductSalesSummaryDto dto)
    {
        var culture = CultureInfo.GetCultureInfo("fa-IR");

        var trend = dto.Trend
            .OrderBy(point => point.PeriodStart)
            .Select(point =>
            {
                var label = point.PeriodStart.ToString("yyyy MMMM", culture);
                return new ProductSalesTrendPointViewModel(label, point.Quantity, point.Revenue);
            })
            .ToArray();

        return new ProductSalesSummaryViewModel(
            dto.TotalOrders,
            dto.TotalQuantity,
            dto.TotalRevenue,
            dto.TotalDiscount,
            dto.AverageOrderValue,
            dto.FirstSaleAt,
            dto.LastSaleAt,
            trend);
    }

    [HttpGet]
    public async Task<IActionResult> Categories(Guid? highlightId = null)
    {
        var cancellationToken = HttpContext.RequestAborted;
        var viewModel = await BuildCategoriesViewModelAsync(cancellationToken, null, null, highlightId);

        ViewData["Title"] = "دسته‌بندی‌های محصول";
        ViewData["Subtitle"] = "چیدمان ساختار فروشگاه، والدین و تگ‌های سئو";
        ViewData["SearchPlaceholder"] = "جستجو در تنظیمات فروشگاه";
        ViewData["ShowSearch"] = false;

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateCategory(ProductCategoryFormModel model)
    {
        var cancellationToken = HttpContext.RequestAborted;

        if (!ModelState.IsValid)
        {
            var viewModel = await BuildCategoriesViewModelAsync(cancellationToken, model, new ProductCategoryUpdateFormModel());
            ViewData["Title"] = "دسته‌بندی‌های محصول";
            ViewData["Subtitle"] = "چیدمان ساختار فروشگاه، والدین و تگ‌های سئو";
            ViewData["SearchPlaceholder"] = "جستجو در تنظیمات فروشگاه";
            ViewData["ShowSearch"] = false;
            return View("Categories", viewModel);
        }

        var command = new CreateSiteCategoryCommand(
            model.Name.Trim(),
            string.IsNullOrWhiteSpace(model.Slug) ? null : model.Slug.Trim(),
            string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim(),
            CategoryScope.Product,
            model.ParentId);

        var result = await _mediator.Send(command, cancellationToken);
        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "خطا در ایجاد دسته‌بندی.");
            var viewModel = await BuildCategoriesViewModelAsync(cancellationToken, model, new ProductCategoryUpdateFormModel());
            ViewData["Title"] = "دسته‌بندی‌های محصول";
            ViewData["Subtitle"] = "چیدمان ساختار فروشگاه، والدین و تگ‌های سئو";
            ViewData["SearchPlaceholder"] = "جستجو در تنظیمات فروشگاه";
            ViewData["ShowSearch"] = false;
            return View("Categories", viewModel);
        }

        TempData["Success"] = "دسته‌بندی جدید با موفقیت ثبت شد.";
        return RedirectToAction(nameof(Categories), new { highlightId = result.Value });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateCategory(ProductCategoryUpdateFormModel model)
    {
        var cancellationToken = HttpContext.RequestAborted;

        if (!ModelState.IsValid)
        {
            var viewModel = await BuildCategoriesViewModelAsync(cancellationToken, new ProductCategoryFormModel(), model, model.Id);
            ViewData["Title"] = "دسته‌بندی‌های محصول";
            ViewData["Subtitle"] = "چیدمان ساختار فروشگاه، والدین و تگ‌های سئو";
            ViewData["SearchPlaceholder"] = "جستجو در تنظیمات فروشگاه";
            ViewData["ShowSearch"] = false;
            return View("Categories", viewModel);
        }

        var command = new UpdateSiteCategoryCommand(
            model.Id,
            model.Name.Trim(),
            string.IsNullOrWhiteSpace(model.Slug) ? null : model.Slug.Trim(),
            string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim(),
            CategoryScope.Product,
            model.ParentId);

        var result = await _mediator.Send(command, cancellationToken);
        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "خطا در بروزرسانی دسته‌بندی.");
            var viewModel = await BuildCategoriesViewModelAsync(cancellationToken, new ProductCategoryFormModel(), model, model.Id);
            ViewData["Title"] = "دسته‌بندی‌های محصول";
            ViewData["Subtitle"] = "چیدمان ساختار فروشگاه، والدین و تگ‌های سئو";
            ViewData["SearchPlaceholder"] = "جستجو در تنظیمات فروشگاه";
            ViewData["ShowSearch"] = false;
            return View("Categories", viewModel);
        }

        TempData["Success"] = "دسته‌بندی با موفقیت بروزرسانی شد.";
        return RedirectToAction(nameof(Categories), new { highlightId = model.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCategory(Guid id)
    {
        var cancellationToken = HttpContext.RequestAborted;
        var command = new DeleteSiteCategoryCommand(id);

        var result = await _mediator.Send(command, cancellationToken);
        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error ?? "حذف دسته‌بندی ممکن نشد.";
            return RedirectToAction(nameof(Categories), new { highlightId = id });
        }

        TempData["Success"] = "دسته‌بندی با موفقیت حذف شد.";
        return RedirectToAction(nameof(Categories));
    }

    [HttpGet]
    public async Task<IActionResult> ExecutionStepsOverview()
    {
        var cancellationToken = HttpContext.RequestAborted;
        var result = await _mediator.Send(new GetProductExecutionStepOverviewQuery(), cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            TempData["Error"] = result.Error ?? "امکان دریافت اطلاعات گام‌های اجرایی وجود ندارد.";
            return RedirectToAction(nameof(Index));
        }

        var viewModel = MapExecutionStepsOverview(result.Value);
        ConfigureProductExecutionStepsOverviewViewData();

        return View("ExecutionStepsOverview", viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> ExecutionSteps(Guid id, Guid? stepId)
    {
        var cancellationToken = HttpContext.RequestAborted;
        var viewModelResult = await BuildProductExecutionStepsViewModelAsync(id, cancellationToken, null, stepId);

        if (!viewModelResult.IsSuccess || viewModelResult.Value is null)
        {
            if (!string.IsNullOrWhiteSpace(viewModelResult.Error))
            {
                TempData["Error"] = viewModelResult.Error;
            }

            return RedirectToAction(nameof(Index));
        }

        ConfigureProductExecutionStepsViewData(viewModelResult.Value.ProductName);
        return View("ExecutionSteps", viewModelResult.Value);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateExecutionStep(
        Guid id,
        [Bind(Prefix = "Form")] ProductExecutionStepFormModel form)
    {
        var cancellationToken = HttpContext.RequestAborted;

        if (!ModelState.IsValid)
        {
            var viewModelResult = await BuildProductExecutionStepsViewModelAsync(id, cancellationToken, form);
            if (!viewModelResult.IsSuccess || viewModelResult.Value is null)
            {
                if (!string.IsNullOrWhiteSpace(viewModelResult.Error))
                {
                    TempData["Error"] = viewModelResult.Error;
                }

                return RedirectToAction(nameof(Index));
            }

            ConfigureProductExecutionStepsViewData(viewModelResult.Value.ProductName);
            return View("ExecutionSteps", viewModelResult.Value);
        }

        var command = new CreateProductExecutionStepCommand(id, form.Title, form.Description, form.Duration, form.Order);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "ثبت گام اجرایی انجام نشد.");
            var viewModelResult = await BuildProductExecutionStepsViewModelAsync(id, cancellationToken, form);
            if (!viewModelResult.IsSuccess || viewModelResult.Value is null)
            {
                if (!string.IsNullOrWhiteSpace(viewModelResult.Error))
                {
                    TempData["Error"] = viewModelResult.Error;
                }

                return RedirectToAction(nameof(Index));
            }

            ConfigureProductExecutionStepsViewData(viewModelResult.Value.ProductName);
            return View("ExecutionSteps", viewModelResult.Value);
        }

        TempData["Success"] = "گام اجرایی با موفقیت ثبت شد.";
        return RedirectToAction(nameof(ExecutionSteps), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateExecutionStep(
        Guid id,
        [Bind(Prefix = "Form")] ProductExecutionStepFormModel form)
    {
        var cancellationToken = HttpContext.RequestAborted;

        if (form.Id is null || form.Id == Guid.Empty)
        {
            ModelState.AddModelError(string.Empty, "شناسه گام معتبر نیست.");
        }

        if (!ModelState.IsValid)
        {
            var viewModelResult = await BuildProductExecutionStepsViewModelAsync(id, cancellationToken, form, form.Id);
            if (!viewModelResult.IsSuccess || viewModelResult.Value is null)
            {
                if (!string.IsNullOrWhiteSpace(viewModelResult.Error))
                {
                    TempData["Error"] = viewModelResult.Error;
                }

                return RedirectToAction(nameof(Index));
            }

            ConfigureProductExecutionStepsViewData(viewModelResult.Value.ProductName);
            return View("ExecutionSteps", viewModelResult.Value);
        }

        var command = new UpdateProductExecutionStepCommand(
            id,
            form.Id!.Value,
            form.Title,
            form.Description,
            form.Duration,
            form.Order);

        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "بروزرسانی گام اجرایی انجام نشد.");
            var viewModelResult = await BuildProductExecutionStepsViewModelAsync(id, cancellationToken, form, form.Id);
            if (!viewModelResult.IsSuccess || viewModelResult.Value is null)
            {
                if (!string.IsNullOrWhiteSpace(viewModelResult.Error))
                {
                    TempData["Error"] = viewModelResult.Error;
                }

                return RedirectToAction(nameof(Index));
            }

            ConfigureProductExecutionStepsViewData(viewModelResult.Value.ProductName);
            return View("ExecutionSteps", viewModelResult.Value);
        }

        TempData["Success"] = "گام اجرایی با موفقیت بروزرسانی شد.";
        return RedirectToAction(nameof(ExecutionSteps), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteExecutionStep(Guid id, Guid stepId)
    {
        if (stepId == Guid.Empty)
        {
            TempData["Error"] = "شناسه گام معتبر نیست.";
            return RedirectToAction(nameof(ExecutionSteps), new { id });
        }

        var cancellationToken = HttpContext.RequestAborted;
        var command = new DeleteProductExecutionStepCommand(id, stepId);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error ?? "حذف گام اجرایی انجام نشد.";
        }
        else
        {
            TempData["Success"] = "گام اجرایی با موفقیت حذف شد.";
        }

        return RedirectToAction(nameof(ExecutionSteps), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> Faqs(Guid id, Guid? faqId)
    {
        var cancellationToken = HttpContext.RequestAborted;
        var viewModelResult = await BuildProductFaqsViewModelAsync(id, cancellationToken, null, faqId);

        if (!viewModelResult.IsSuccess || viewModelResult.Value is null)
        {
            if (!string.IsNullOrWhiteSpace(viewModelResult.Error))
            {
                TempData["Error"] = viewModelResult.Error;
            }

            return RedirectToAction(nameof(Index));
        }

        ConfigureProductFaqsViewData(viewModelResult.Value.ProductName);
        return View("Faqs", viewModelResult.Value);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateFaq(Guid id, [Bind(Prefix = "Form")] ProductFaqFormModel form)
    {
        var cancellationToken = HttpContext.RequestAborted;

        if (!ModelState.IsValid)
        {
            var viewModelResult = await BuildProductFaqsViewModelAsync(id, cancellationToken, form);
            if (!viewModelResult.IsSuccess || viewModelResult.Value is null)
            {
                if (!string.IsNullOrWhiteSpace(viewModelResult.Error))
                {
                    TempData["Error"] = viewModelResult.Error;
                }

                return RedirectToAction(nameof(Index));
            }

            ConfigureProductFaqsViewData(viewModelResult.Value.ProductName);
            return View("Faqs", viewModelResult.Value);
        }

        var command = new CreateProductFaqCommand(id, form.Question, form.Answer, form.Order);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "ثبت سوال متداول انجام نشد.");
            var viewModelResult = await BuildProductFaqsViewModelAsync(id, cancellationToken, form);
            if (!viewModelResult.IsSuccess || viewModelResult.Value is null)
            {
                if (!string.IsNullOrWhiteSpace(viewModelResult.Error))
                {
                    TempData["Error"] = viewModelResult.Error;
                }

                return RedirectToAction(nameof(Index));
            }

            ConfigureProductFaqsViewData(viewModelResult.Value.ProductName);
            return View("Faqs", viewModelResult.Value);
        }

        TempData["Success"] = "سوال متداول با موفقیت ثبت شد.";
        return RedirectToAction(nameof(Faqs), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateFaq(Guid id, [Bind(Prefix = "Form")] ProductFaqFormModel form)
    {
        var cancellationToken = HttpContext.RequestAborted;

        if (form.Id is null || form.Id == Guid.Empty)
        {
            ModelState.AddModelError(string.Empty, "شناسه سوال معتبر نیست.");
        }

        if (!ModelState.IsValid)
        {
            var viewModelResult = await BuildProductFaqsViewModelAsync(id, cancellationToken, form, form.Id);
            if (!viewModelResult.IsSuccess || viewModelResult.Value is null)
            {
                if (!string.IsNullOrWhiteSpace(viewModelResult.Error))
                {
                    TempData["Error"] = viewModelResult.Error;
                }

                return RedirectToAction(nameof(Index));
            }

            ConfigureProductFaqsViewData(viewModelResult.Value.ProductName);
            return View("Faqs", viewModelResult.Value);
        }

        var command = new UpdateProductFaqCommand(
            id,
            form.Id!.Value,
            form.Question,
            form.Answer,
            form.Order);

        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "بروزرسانی سوال متداول انجام نشد.");
            var viewModelResult = await BuildProductFaqsViewModelAsync(id, cancellationToken, form, form.Id);
            if (!viewModelResult.IsSuccess || viewModelResult.Value is null)
            {
                if (!string.IsNullOrWhiteSpace(viewModelResult.Error))
                {
                    TempData["Error"] = viewModelResult.Error;
                }

                return RedirectToAction(nameof(Index));
            }

            ConfigureProductFaqsViewData(viewModelResult.Value.ProductName);
            return View("Faqs", viewModelResult.Value);
        }

        TempData["Success"] = "سوال متداول با موفقیت بروزرسانی شد.";
        return RedirectToAction(nameof(Faqs), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteFaq(Guid id, Guid faqId)
    {
        if (faqId == Guid.Empty)
        {
            TempData["Error"] = "شناسه سوال معتبر نیست.";
            return RedirectToAction(nameof(Faqs), new { id });
        }

        var cancellationToken = HttpContext.RequestAborted;
        var command = new DeleteProductFaqCommand(id, faqId);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error ?? "حذف سوال متداول انجام نشد.";
        }
        else
        {
            TempData["Success"] = "سوال متداول با موفقیت حذف شد.";
        }

        return RedirectToAction(nameof(Faqs), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> Comments(Guid id)
    {
        if (id == Guid.Empty)
        {
            TempData["Error"] = "شناسه محصول معتبر نیست.";
            return RedirectToAction(nameof(Index));
        }

        var cancellationToken = HttpContext.RequestAborted;
        var result = await _mediator.Send(new GetProductCommentsQuery(id), cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            TempData["Error"] = result.Error ?? "امکان نمایش نظرات این محصول وجود ندارد.";
            return RedirectToAction(nameof(Index));
        }

        var viewModel = MapProductComments(result.Value);

        ViewData["Title"] = "مدیریت نظرات محصول";
        ViewData["Subtitle"] = $"دیدگاه‌های ثبت شده برای «{viewModel.ProductName}»";

        return View("Comments", viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ModerateComment(Guid id, Guid commentId, bool approve)
    {
        if (id == Guid.Empty || commentId == Guid.Empty)
        {
            TempData["Error"] = "درخواست ارسال شده معتبر نیست.";
            return RedirectToAction(nameof(Index));
        }

        var cancellationToken = HttpContext.RequestAborted;
        var result = await _mediator.Send(new SetProductCommentApprovalCommand(id, commentId, approve), cancellationToken);

        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error ?? "امکان بروزرسانی وضعیت نظر وجود ندارد.";
        }
        else
        {
            TempData["Success"] = approve
                ? "نظر با موفقیت تایید شد."
                : "وضعیت نظر به رد شده تغییر کرد.";
        }

        return RedirectToAction(nameof(Comments), new { id });
    }

    private void ConfigureProductIndexViewData()
    {
        ViewData["Title"] = "مدیریت محصولات";
        ViewData["Subtitle"] = "نظارت بر وضعیت انتشار، موجودی و عملکرد فروش";
        ViewData["SearchPlaceholder"] = "جستجو در محصولات";
        ViewData["ShowSearch"] = true;
    }

    private void ConfigureProductExecutionStepsViewData(string productName)
    {
        ViewData["Title"] = $"گام‌های اجرایی {productName}";
        ViewData["Subtitle"] = "ساختار برنامه و مراحل اجرایی این محصول را مدیریت کنید.";
        ViewData["SearchPlaceholder"] = "جستجو در محصولات";
        ViewData["ShowSearch"] = false;
    }

    private void ConfigureProductExecutionStepsOverviewViewData()
    {
        ViewData["Title"] = "گام‌های اجرایی محصولات";
        ViewData["Subtitle"] = "پایش و مدیریت ساختار اجرایی همه محصولات";
        ViewData["SearchPlaceholder"] = "جستجو در محصولات";
        ViewData["ShowSearch"] = false;
    }

    private void ConfigureProductFaqsViewData(string productName)
    {
        ViewData["Title"] = $"سوالات متداول {productName}";
        ViewData["Subtitle"] = "پرسش‌های پرتکرار مشتریان این محصول را مدیریت کنید.";
        ViewData["SearchPlaceholder"] = "جستجو در محصولات";
        ViewData["ShowSearch"] = false;
    }

    private async Task<ProductIndexViewModel> BuildProductIndexViewModelAsync(
        ProductIndexRequest request,
        CancellationToken cancellationToken)
    {
        var lookupsResult = await _mediator.Send(new GetProductLookupsQuery(), cancellationToken);
        if (!lookupsResult.IsSuccess && !string.IsNullOrWhiteSpace(lookupsResult.Error))
        {
            TempData["Error"] = lookupsResult.Error;
        }

        var listResult = await _mediator.Send(new GetProductListQuery(
            request.Search,
            request.CategoryId,
            request.Type,
            request.IsPublished,
            request.MinPrice,
            request.MaxPrice,
            request.Page,
            request.PageSize), cancellationToken);

        if (!listResult.IsSuccess && !string.IsNullOrWhiteSpace(listResult.Error))
        {
            TempData["Error"] = listResult.Error;
        }

        var lookups = lookupsResult.IsSuccess && lookupsResult.Value is not null
            ? lookupsResult.Value
            : new ProductLookupsDto(Array.Empty<SiteCategoryDto>(), Array.Empty<string>());

        var list = listResult.IsSuccess && listResult.Value is not null
            ? listResult.Value
            : new ProductListResultDto(
                Array.Empty<ProductListItemDto>(),
                0,
                0,
                request.Page <= 0 ? 1 : request.Page,
                request.PageSize <= 0 ? 12 : request.PageSize,
                1);

        var filters = new ProductIndexFilterViewModel
        {
            Search = request.Search,
            CategoryId = request.CategoryId,
            Type = request.Type,
            IsPublished = request.IsPublished,
            MinPrice = request.MinPrice,
            MaxPrice = request.MaxPrice,
            PageSize = list.PageSize
        };

        var products = list.Items
            .Select(MapListItem)
            .ToArray();

        var statistics = BuildStatistics(list, products);
        var categories = lookups.Categories ?? Array.Empty<SiteCategoryDto>();
        var categoryOptions = BuildCategoryOptions(categories, request.CategoryId, includeAllOption: true);
        var typeOptions = BuildTypeOptions(request.Type);
        var statusOptions = BuildStatusOptions(request.IsPublished);

        var firstItemIndex = list.FilteredCount == 0 ? 0 : ((list.PageNumber - 1) * list.PageSize) + 1;
        var lastItemIndex = list.FilteredCount == 0 ? 0 : firstItemIndex + products.Length - 1;

        return new ProductIndexViewModel
        {
            Products = products,
            Statistics = statistics,
            Filters = filters,
            CategoryOptions = categoryOptions,
            TypeOptions = typeOptions,
            StatusOptions = statusOptions,
            PageSizeOptions = PageSizeOptions,
            TagSuggestions = (lookups.Tags ?? Array.Empty<string>()).Take(20).ToArray(),
            TotalCount = list.TotalCount,
            FilteredCount = list.FilteredCount,
            PageNumber = list.PageNumber,
            PageSize = list.PageSize,
            TotalPages = list.TotalPages,
            FirstItemIndex = firstItemIndex,
            LastItemIndex = lastItemIndex
        };
    }

    private async Task<ProductFormViewModel> BuildProductFormViewModelAsync(
        ProductFormViewModel model,
        CancellationToken cancellationToken)
    {
        var lookupsResult = await _mediator.Send(new GetProductLookupsQuery(), cancellationToken);
        if (!lookupsResult.IsSuccess && !string.IsNullOrWhiteSpace(lookupsResult.Error))
        {
            TempData["Error"] = lookupsResult.Error;
        }

        var lookups = lookupsResult.IsSuccess && lookupsResult.Value is not null
            ? lookupsResult.Value
            : new ProductLookupsDto(Array.Empty<SiteCategoryDto>(), Array.Empty<string>());

        var categories = lookups.Categories ?? Array.Empty<SiteCategoryDto>();

        var teacherLookupsResult = await _mediator.Send(new GetTeacherLookupsQuery(), cancellationToken);
        if (!teacherLookupsResult.IsSuccess && !string.IsNullOrWhiteSpace(teacherLookupsResult.Error))
        {
            TempData["Warning"] = teacherLookupsResult.Error;
        }

        var teacherLookups = teacherLookupsResult.IsSuccess && teacherLookupsResult.Value is not null
            ? teacherLookupsResult.Value
            : Array.Empty<TeacherLookupDto>();

        model.Selections = new ProductFormSelectionsViewModel
        {
            CategoryOptions = BuildCategoryOptions(categories, model.CategoryId, includeAllOption: false),
            TypeOptions = BuildProductTypeFormOptions(model.Type),
            PublishStatusOptions = BuildPublishStatusOptions(model.PublishStatus),
            RobotsOptions = BuildRobotsOptions(model.Robots),
            TeacherOptions = BuildTeacherOptions(teacherLookups, model.TeacherId)
        };

        EnsureGalleryInitialized(model);
        PopulatePublishedFields(model);

        return model;
    }

    private async Task<Result<ProductExecutionStepsViewModel>> BuildProductExecutionStepsViewModelAsync(
        Guid productId,
        CancellationToken cancellationToken,
        ProductExecutionStepFormModel? form = null,
        Guid? editingStepId = null)
    {
        var result = await _mediator.Send(new GetProductExecutionStepsQuery(productId), cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return Result<ProductExecutionStepsViewModel>.Failure(result.Error ?? "امکان دریافت اطلاعات محصول وجود ندارد.");
        }

        var viewModel = MapExecutionSteps(result.Value, form, editingStepId);
        return Result<ProductExecutionStepsViewModel>.Success(viewModel);
    }

    private async Task<Result<ProductFaqsViewModel>> BuildProductFaqsViewModelAsync(
        Guid productId,
        CancellationToken cancellationToken,
        ProductFaqFormModel? form = null,
        Guid? editingFaqId = null)
    {
        var result = await _mediator.Send(new GetProductFaqsQuery(productId), cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return Result<ProductFaqsViewModel>.Failure(result.Error ?? "امکان دریافت اطلاعات محصول وجود ندارد.");
        }

        var viewModel = MapFaqs(result.Value, form, editingFaqId);
        return Result<ProductFaqsViewModel>.Success(viewModel);
    }

    private void ConfigureProductFormViewData(bool isEdit)
    {
        ViewData["Title"] = isEdit ? "ویرایش محصول" : "افزودن محصول جدید";
        ViewData["Subtitle"] = isEdit
            ? "به‌روزرسانی اطلاعات محصول، وضعیت انتشار و سئو"
            : "تکمیل جزئیات محصول، تصاویر و تنظیمات سئو";
        ViewData["SearchPlaceholder"] = "جستجو در محصولات";
        ViewData["ShowSearch"] = false;
    }

    private static void NormalizeProductFormModel(ProductFormViewModel model)
    {
        if (model is null)
        {
            return;
        }

        model.Name = model.Name?.Trim() ?? string.Empty;
        model.Summary = string.IsNullOrWhiteSpace(model.Summary) ? null : model.Summary.Trim();
        model.Description = model.Description?.Trim() ?? string.Empty;
        model.SeoTitle = model.SeoTitle?.Trim() ?? string.Empty;
        model.SeoDescription = string.IsNullOrWhiteSpace(model.SeoDescription) ? null : model.SeoDescription.Trim();
        model.SeoKeywords = string.IsNullOrWhiteSpace(model.SeoKeywords) ? null : model.SeoKeywords.Trim();
        model.SeoSlug = model.SeoSlug?.Trim() ?? string.Empty;
        model.Robots = string.IsNullOrWhiteSpace(model.Robots) ? "index,follow" : model.Robots.Trim();
        model.FeaturedImagePath = string.IsNullOrWhiteSpace(model.FeaturedImagePath) ? null : model.FeaturedImagePath.Trim();
        model.DigitalDownloadPath = string.IsNullOrWhiteSpace(model.DigitalDownloadPath) ? null : model.DigitalDownloadPath.Trim();
        model.Tags = string.IsNullOrWhiteSpace(model.Tags) ? string.Empty : model.Tags.Trim();
        model.TeacherId = string.IsNullOrWhiteSpace(model.TeacherId) ? null : model.TeacherId.Trim();

        if (model.CompareAtPrice is < 0)
        {
            model.CompareAtPrice = null;
        }

        if (model.StockQuantity < 0)
        {
            model.StockQuantity = 0;
        }

        EnsureGalleryInitialized(model);
    }

    private static void EnsureGalleryInitialized(ProductFormViewModel model)
    {
        if (model is null)
        {
            return;
        }

        model.Gallery ??= new List<ProductGalleryItemFormModel>();
        if (model.Gallery.Count == 0)
        {
            model.Gallery.Add(new ProductGalleryItemFormModel());
        }
    }

    private static IReadOnlyCollection<SelectListItem> BuildProductTypeFormOptions(ProductType selectedType)
    {
        return new[]
        {
            new SelectListItem("محصول فیزیکی", ProductType.Physical.ToString(), selectedType == ProductType.Physical),
            new SelectListItem("محصول دانلودی", ProductType.Digital.ToString(), selectedType == ProductType.Digital)
        };
    }

    private static IReadOnlyCollection<SelectListItem> BuildPublishStatusOptions(ProductPublishStatus selected)
    {
        return new[]
        {
            new SelectListItem("پیش‌نویس", ProductPublishStatus.Draft.ToString(), selected == ProductPublishStatus.Draft),
            new SelectListItem("منتشر شده", ProductPublishStatus.Published.ToString(), selected == ProductPublishStatus.Published),
            new SelectListItem("زمان‌بندی شده", ProductPublishStatus.Scheduled.ToString(), selected == ProductPublishStatus.Scheduled)
        };
    }

    private static IReadOnlyCollection<SelectListItem> BuildRobotsOptions(params string?[] additionalValues)
    {
        var extras = additionalValues?
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!.Trim())
            .ToArray() ?? Array.Empty<string>();

        var values = RobotsOptionValues
            .Concat(extras)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return values.Select(value => new SelectListItem(value, value)).ToArray();
    }

    private static IReadOnlyCollection<SelectListItem> BuildTeacherOptions(
        IReadOnlyCollection<TeacherLookupDto> teachers,
        string? selectedTeacherId)
    {
        var options = new List<SelectListItem>
        {
            new("ثبت به نام سایت", string.Empty, string.IsNullOrWhiteSpace(selectedTeacherId))
        };

        if (teachers is not null)
        {
            foreach (var teacher in teachers)
            {
                if (string.IsNullOrWhiteSpace(teacher.UserId))
                {
                    continue;
                }

                var label = string.IsNullOrWhiteSpace(teacher.Degree)
                    ? teacher.DisplayName
                    : $"{teacher.DisplayName} - {teacher.Degree}";

                var isSelected = !string.IsNullOrWhiteSpace(selectedTeacherId)
                    && string.Equals(teacher.UserId, selectedTeacherId, StringComparison.OrdinalIgnoreCase);

                if (!teacher.IsActive)
                {
                    label = $"{label} (غیرفعال)";
                }

                var option = new SelectListItem(label, teacher.UserId, isSelected);

                options.Add(option);
            }
        }

        if (!string.IsNullOrWhiteSpace(selectedTeacherId)
            && options.All(option => !string.Equals(option.Value, selectedTeacherId, StringComparison.OrdinalIgnoreCase)))
        {
            options.Add(new SelectListItem(
                $"کاربر با شناسه {selectedTeacherId} در فهرست مدرسین فعال موجود نیست",
                selectedTeacherId,
                true));
        }

        return options;
    }

    private static IReadOnlyCollection<string> ParseTags(string? rawTags)
    {
        if (string.IsNullOrWhiteSpace(rawTags))
        {
            return Array.Empty<string>();
        }

        var separators = new[] { ',', '،', ';', '|', '\n', '\r' };
        const int MaxTagLength = 50;

        return rawTags
            .Split(separators, StringSplitOptions.RemoveEmptyEntries)
            .Select(tag => tag.Trim())
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Select(tag => tag.Length > MaxTagLength ? tag[..MaxTagLength] : tag)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static ProductPublishStatus DeterminePublishStatus(bool isPublished, DateTimeOffset? publishedAt)
    {
        if (!isPublished)
        {
            return ProductPublishStatus.Draft;
        }

        if (publishedAt.HasValue && publishedAt.Value > DateTimeOffset.UtcNow)
        {
            return ProductPublishStatus.Scheduled;
        }

        return ProductPublishStatus.Published;
    }

    private static ProductListItemViewModel MapListItem(ProductListItemDto item)
    {
        return new ProductListItemViewModel(
            item.Id,
            item.Name,
            item.CategoryName,
            item.CategoryId,
            item.Type,
            item.Price,
            item.CompareAtPrice,
            item.IsPublished,
            item.PublishedAt,
            item.FeaturedImagePath,
            item.TagList,
            item.UpdatedAt);
    }

    private static ProductExecutionStepsViewModel MapExecutionSteps(
        ProductExecutionStepsDto dto,
        ProductExecutionStepFormModel? form,
        Guid? editingStepId)
    {
        var steps = dto.Steps
            .OrderBy(step => step.DisplayOrder)
            .ThenBy(step => step.Title, StringComparer.CurrentCulture)
            .Select(MapExecutionStep)
            .ToArray();

        var defaultOrder = steps.Length == 0 ? 0 : steps.Max(step => step.Order) + 1;

        ProductExecutionStepFormModel formModel;
        if (form is not null)
        {
            formModel = new ProductExecutionStepFormModel
            {
                Id = form.Id,
                Title = form.Title,
                Description = form.Description,
                Duration = form.Duration,
                Order = form.Order
            };
        }
        else if (editingStepId.HasValue)
        {
            var editing = steps.FirstOrDefault(step => step.Id == editingStepId.Value);
            formModel = editing is null
                ? new ProductExecutionStepFormModel { Order = defaultOrder }
                : new ProductExecutionStepFormModel
                {
                    Id = editing.Id,
                    Title = editing.Title,
                    Description = editing.Description,
                    Duration = editing.Duration,
                    Order = editing.Order
                };
        }
        else
        {
            formModel = new ProductExecutionStepFormModel { Order = defaultOrder };
        }

        if (formModel.Order < 0)
        {
            formModel.Order = 0;
        }

        var highlightId = editingStepId ?? form?.Id;

        return new ProductExecutionStepsViewModel
        {
            ProductId = dto.ProductId,
            ProductName = dto.ProductName,
            Steps = steps,
            Form = formModel,
            HighlightedStepId = highlightId
        };
    }

    private static ProductExecutionStepsOverviewViewModel MapExecutionStepsOverview(
        ProductExecutionStepsOverviewDto dto)
    {
        var items = dto.Items
            .OrderByDescending(item => item.StepCount)
            .ThenBy(item => item.ProductName, StringComparer.CurrentCulture)
            .Select(item => new ProductExecutionStepSummaryViewModel(
                item.ProductId,
                item.ProductName,
                item.CategoryName,
                item.Type,
                item.IsPublished,
                item.StepCount,
                item.UpdatedAt))
            .ToArray();

        return new ProductExecutionStepsOverviewViewModel
        {
            TotalProducts = dto.TotalProducts,
            ProductsWithSteps = dto.ProductsWithSteps,
            TotalSteps = dto.TotalSteps,
            AverageStepsPerProduct = dto.AverageStepsPerProduct,
            Items = items
        };
    }

    private static ProductExecutionStepListItemViewModel MapExecutionStep(ProductExecutionStepDto dto)
        => new(dto.Id, dto.Title, dto.Description, dto.Duration, dto.DisplayOrder);

    private static ProductFaqsViewModel MapFaqs(
        ProductFaqsDto dto,
        ProductFaqFormModel? form,
        Guid? editingFaqId)
    {
        var faqs = dto.Items
            .OrderBy(faq => faq.DisplayOrder)
            .ThenBy(faq => faq.Question, StringComparer.CurrentCulture)
            .Select(MapFaq)
            .ToArray();

        var defaultOrder = faqs.Length == 0 ? 0 : faqs.Max(faq => faq.Order) + 1;

        ProductFaqFormModel formModel;
        if (form is not null)
        {
            formModel = new ProductFaqFormModel
            {
                Id = form.Id,
                Question = form.Question,
                Answer = form.Answer,
                Order = form.Order
            };
        }
        else if (editingFaqId.HasValue)
        {
            var editing = faqs.FirstOrDefault(faq => faq.Id == editingFaqId.Value);
            formModel = editing is null
                ? new ProductFaqFormModel { Order = defaultOrder }
                : new ProductFaqFormModel
                {
                    Id = editing.Id,
                    Question = editing.Question,
                    Answer = editing.Answer,
                    Order = editing.Order
                };
        }
        else
        {
            formModel = new ProductFaqFormModel { Order = defaultOrder };
        }

        if (formModel.Order < 0)
        {
            formModel.Order = 0;
        }

        var highlightId = editingFaqId ?? form?.Id;

        return new ProductFaqsViewModel
        {
            ProductId = dto.ProductId,
            ProductName = dto.ProductName,
            Faqs = faqs,
            Form = formModel,
            HighlightedFaqId = highlightId
        };
    }

    private static ProductCommentListViewModel MapProductComments(ProductCommentListResultDto dto)
    {
        var comments = dto.Comments?.ToArray() ?? Array.Empty<ProductCommentDto>();
        var lookup = comments.ToDictionary(comment => comment.Id);

        static string? BuildExcerpt(string? content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return null;
            }

            var normalized = content.Trim();
            if (normalized.Length <= 80)
            {
                return normalized;
            }

            return normalized[..77] + "…";
        }

        var items = comments
            .Select(comment =>
            {
                ProductCommentDto? parent = null;
                if (comment.ParentId.HasValue)
                {
                    lookup.TryGetValue(comment.ParentId.Value, out parent);
                }

                return new ProductCommentItemViewModel
                {
                    Id = comment.Id,
                    ParentId = comment.ParentId,
                    AuthorName = comment.AuthorName,
                    Content = comment.Content,
                    Rating = comment.Rating,
                    IsApproved = comment.IsApproved,
                    CreatedAt = comment.CreatedAt,
                    UpdatedAt = comment.UpdatedAt,
                    ApprovedByName = comment.ApprovedByName,
                    ApprovedAt = comment.ApprovedAt,
                    ParentAuthorName = parent?.AuthorName,
                    ParentExcerpt = BuildExcerpt(parent?.Content)
                };
            })
            .OrderByDescending(comment => comment.CreatedAt)
            .ToArray();

        var approvedCount = items.Count(comment => comment.IsApproved);
        var averageRating = items.Length == 0
            ? 0
            : Math.Round(items.Average(comment => comment.Rating), 1, MidpointRounding.AwayFromZero);

        return new ProductCommentListViewModel
        {
            ProductId = dto.Product.Id,
            ProductName = dto.Product.Name,
            ProductSlug = dto.Product.SeoSlug,
            TotalCount = items.Length,
            ApprovedCount = approvedCount,
            PendingCount = items.Length - approvedCount,
            AverageRating = averageRating,
            Comments = items
        };
    }

    private static ProductFaqListItemViewModel MapFaq(ProductFaqDto dto)
        => new(dto.Id, dto.Question, dto.Answer, dto.DisplayOrder);

    private static ProductListStatisticsViewModel BuildStatistics(ProductListResultDto list, IReadOnlyCollection<ProductListItemViewModel> products)
    {
        var publishedCount = products.Count(product => product.IsPublished);
        var draftCount = products.Count - publishedCount;
        var physicalCount = products.Count(product => product.Type == ProductType.Physical);
        var digitalCount = products.Count(product => product.Type == ProductType.Digital);
        var averagePrice = products.Count == 0 ? 0 : Math.Round(products.Average(product => product.Price), 0);
        var highestPrice = products.Count == 0 ? 0 : products.Max(product => product.Price);
        var lowestPrice = products.Count == 0 ? 0 : products.Min(product => product.Price);

        return new ProductListStatisticsViewModel(
            list.TotalCount,
            list.FilteredCount,
            publishedCount,
            draftCount,
            physicalCount,
            digitalCount,
            averagePrice,
            highestPrice,
            lowestPrice);
    }

    private void PopulatePublishedFields(ProductFormViewModel model)
    {
        if (model is null)
        {
            return;
        }

        if (model.PublishedAt.HasValue)
        {
            var local = model.PublishedAt.Value.ToLocalTime();
            var calendar = new PersianCalendar();
            var persianDate = string.Format(
                CultureInfo.InvariantCulture,
                "{0:0000}-{1:00}-{2:00}",
                calendar.GetYear(local.DateTime),
                calendar.GetMonth(local.DateTime),
                calendar.GetDayOfMonth(local.DateTime));

            if (string.IsNullOrWhiteSpace(model.PublishedAtPersian))
            {
                model.PublishedAtPersian = persianDate;
            }

            if (string.IsNullOrWhiteSpace(model.PublishedAtTime))
            {
                model.PublishedAtTime = local.ToString("HH:mm", CultureInfo.InvariantCulture);
            }
        }
        else
        {
            if (string.IsNullOrWhiteSpace(model.PublishedAtPersian))
            {
                model.PublishedAtPersian = string.Empty;
            }

            if (string.IsNullOrWhiteSpace(model.PublishedAtTime))
            {
                model.PublishedAtTime = string.Empty;
            }
        }
    }

    private bool TryResolvePublishState(ProductFormViewModel model, out bool isPublished, out DateTimeOffset? publishedAt)
    {
        isPublished = false;
        publishedAt = null;

        if (model is null)
        {
            return true;
        }

        switch (model.PublishStatus)
        {
            case ProductPublishStatus.Draft:
                model.PublishedAt = null;
                model.PublishedAtPersian = string.Empty;
                model.PublishedAtTime = string.Empty;
                return true;
            case ProductPublishStatus.Published:
                if (!TryConvertPublishedAt(model, requireDate: false, out publishedAt))
                {
                    return false;
                }

                if (publishedAt is null)
                {
                    publishedAt = DateTimeOffset.UtcNow;
                    model.PublishedAt = publishedAt;
                }

                isPublished = true;
                return true;
            case ProductPublishStatus.Scheduled:
                if (!TryConvertPublishedAt(model, requireDate: true, out publishedAt))
                {
                    return false;
                }

                if (publishedAt is null)
                {
                    ModelState.AddModelError(nameof(ProductFormViewModel.PublishedAtPersian), "تاریخ انتشار را وارد کنید.");
                    return false;
                }

                isPublished = true;
                return true;
            default:
                ModelState.AddModelError(nameof(ProductFormViewModel.PublishStatus), "وضعیت انتشار انتخاب شده معتبر نیست.");
                return false;
        }
    }

    private bool TryConvertPublishedAt(ProductFormViewModel model, bool requireDate, out DateTimeOffset? publishedAt)
    {
        publishedAt = null;

        var rawDateInput = model.PublishedAtPersian;
        var normalizedDate = NormalizePersianDateInput(rawDateInput);
        var dateProvided = !string.IsNullOrWhiteSpace(rawDateInput);
        model.PublishedAtPersian = normalizedDate;

        if (!TryNormalizeTimeInput(model.PublishedAtTime, out var hour, out var minute, out var normalizedTime, out var timeError))
        {
            model.PublishedAtTime = normalizedTime;
            ModelState.AddModelError(nameof(ProductFormViewModel.PublishedAtTime), timeError ?? "ساعت انتشار وارد شده معتبر نیست.");
            return false;
        }

        model.PublishedAtTime = normalizedTime;

        if (string.IsNullOrEmpty(normalizedDate))
        {
            if (requireDate)
            {
                ModelState.AddModelError(nameof(ProductFormViewModel.PublishedAtPersian), "تاریخ انتشار را وارد کنید.");
                return false;
            }

            if (dateProvided && !string.IsNullOrWhiteSpace(model.PublishedAtTime))
            {
                ModelState.AddModelError(nameof(ProductFormViewModel.PublishedAtPersian), "تاریخ انتشار وارد شده معتبر نیست.");
                return false;
            }

            model.PublishedAt = null;
            return true;
        }

        if (!TryExtractPersianDateParts(normalizedDate, out var year, out var month, out var day))
        {
            ModelState.AddModelError(nameof(ProductFormViewModel.PublishedAtPersian), "تاریخ انتشار وارد شده معتبر نیست.");
            return false;
        }

        try
        {
            var persianDateTime = new global::PersianDateTime(year, month, day, hour, minute, 0);
            var gregorian = persianDateTime.ToDateTime();
            var offset = GetIranOffset(gregorian);
            publishedAt = new DateTimeOffset(DateTime.SpecifyKind(gregorian, DateTimeKind.Unspecified), offset);
            model.PublishedAt = publishedAt;
            return true;
        }
        catch
        {
            ModelState.AddModelError(nameof(ProductFormViewModel.PublishedAtPersian), "تاریخ انتشار وارد شده معتبر نیست.");
            return false;
        }
    }

    private void ValidateDigitalFile(IFormFile? file, string fieldName)
    {
        if (file is null || file.Length == 0)
        {
            return;
        }

        if (!_fileSettingServices.IsFileSizeValid(file, MaxDigitalFileSizeKb))
        {
            ModelState.AddModelError(fieldName, "حجم فایل دانلودی باید کمتر از ۲۰۰ مگابایت باشد.");
        }

        var extension = Path.GetExtension(file.FileName);
        if (!string.IsNullOrWhiteSpace(extension) && !AllowedDigitalExtensions.Contains(extension))
        {
            ModelState.AddModelError(fieldName, "فرمت فایل انتخاب شده پشتیبانی نمی‌شود.");
        }
    }

    private void ValidateFeaturedImage(IFormFile? file, string fieldName)
    {
        if (file is null || file.Length == 0)
        {
            return;
        }

        if (!_fileSettingServices.IsFileSizeValid(file, MaxImageSizeKb))
        {
            ModelState.AddModelError(fieldName, "حجم تصویر باید کمتر از ۵ مگابایت باشد.");
        }

        var contentType = file.ContentType ?? string.Empty;
        if (!AllowedImageContentTypes.Contains(contentType))
        {
            ModelState.AddModelError(fieldName, "فرمت تصویر پشتیبانی نمی‌شود.");
        }
    }

    private void ValidateGalleryFiles(IList<ProductGalleryItemFormModel>? gallery)
    {
        if (gallery is null)
        {
            return;
        }

        for (var index = 0; index < gallery.Count; index++)
        {
            var item = gallery[index];
            if (item is null || item.Remove)
            {
                continue;
            }

            var file = item.Image;
            if (file is null || file.Length == 0)
            {
                continue;
            }

            if (!_fileSettingServices.IsFileSizeValid(file, MaxImageSizeKb))
            {
                ModelState.AddModelError($"Gallery[{index}].Image", "حجم تصویر باید کمتر از ۵ مگابایت باشد.");
            }

            var contentType = file.ContentType ?? string.Empty;
            if (!AllowedImageContentTypes.Contains(contentType))
            {
                ModelState.AddModelError($"Gallery[{index}].Image", "فرمت تصویر پشتیبانی نمی‌شود.");
            }
        }
    }

    private async Task<List<(string Path, int Order)>?> SaveGalleryImagesAsync(
        IList<ProductGalleryItemFormModel>? gallery,
        CancellationToken cancellationToken)
    {
        var results = new List<(string Path, int Order)>();

        if (gallery is null)
        {
            return results;
        }

        for (var index = 0; index < gallery.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var item = gallery[index];
            if (item is null)
            {
                continue;
            }

            if (item.Remove)
            {
                item.Path = null;
                continue;
            }

            string? path = null;

            if (item.Image is { Length: > 0 })
            {
                path = await SaveGalleryImageAsync(item.Image, $"Gallery[{index}].Image");
                if (path is null)
                {
                    return null;
                }

                item.Path = path;
                item.Image = null;
            }
            else if (!string.IsNullOrWhiteSpace(item.Path))
            {
                path = item.Path.Trim();
            }

            if (string.IsNullOrWhiteSpace(path))
            {
                continue;
            }

            results.Add((path, item.Order));
        }

        return results;
    }

    private Task<string?> SaveDigitalFileAsync(IFormFile? file, string fieldName)
    {
        if (file is null || file.Length == 0)
        {
            return Task.FromResult<string?>(null);
        }

        var response = _fileSettingServices.UploadFile(DigitalUploadFolder, file, Guid.NewGuid().ToString("N"));

        if (!response.Success || string.IsNullOrWhiteSpace(response.Data))
        {
            ModelState.AddModelError(fieldName, response.Messages.FirstOrDefault()?.message ?? "امکان ذخیره‌سازی فایل وجود ندارد.");
            return Task.FromResult<string?>(null);
        }

        var path = response.Data.Replace("\\", "/", StringComparison.Ordinal);
        return Task.FromResult<string?>(path);
    }

    private Task<string?> SaveFeaturedImageAsync(IFormFile? file, string fieldName)
    {
        if (file is null || file.Length == 0)
        {
            return Task.FromResult<string?>(null);
        }

        var response = _fileSettingServices.UploadImage(FeaturedUploadFolder, file, Guid.NewGuid().ToString("N"));

        if (!response.Success || string.IsNullOrWhiteSpace(response.Data))
        {
            ModelState.AddModelError(fieldName, response.Messages.FirstOrDefault()?.message ?? "امکان ذخیره‌سازی تصویر وجود ندارد.");
            return Task.FromResult<string?>(null);
        }

        var path = response.Data.Replace("\\", "/", StringComparison.Ordinal);
        return Task.FromResult<string?>(path);
    }

    private Task<string?> SaveGalleryImageAsync(IFormFile file, string fieldName)
    {
        if (file is null || file.Length == 0)
        {
            return Task.FromResult<string?>(null);
        }

        var response = _fileSettingServices.UploadImage(GalleryUploadFolder, file, Guid.NewGuid().ToString("N"));

        if (!response.Success || string.IsNullOrWhiteSpace(response.Data))
        {
            ModelState.AddModelError(fieldName, response.Messages.FirstOrDefault()?.message ?? "امکان ذخیره‌سازی تصویر وجود ندارد.");
            return Task.FromResult<string?>(null);
        }

        var path = response.Data.Replace("\\", "/", StringComparison.Ordinal);
        return Task.FromResult<string?>(path);
    }

    private static string NormalizeRobots(string? robots)
    {
        if (string.IsNullOrWhiteSpace(robots))
        {
            return "index,follow";
        }

        return robots.Replace(" ", string.Empty, StringComparison.Ordinal).Trim().ToLowerInvariant();
    }

    private static bool IsRobotsOptionValid(string robots)
        => RobotsOptionValues.Any(option => string.Equals(option, robots, StringComparison.OrdinalIgnoreCase));

    private static string NormalizePersianDateInput(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalizedDigits = NormalizeDigits(value)
            .Replace("\u200f", string.Empty, StringComparison.Ordinal)
            .Replace("\u200e", string.Empty, StringComparison.Ordinal)
            .Replace(".", "/", StringComparison.Ordinal)
            .Replace("-", "/", StringComparison.Ordinal)
            .Replace("\\", "/", StringComparison.Ordinal)
            .Replace(" ", string.Empty, StringComparison.Ordinal);

        var parts = normalizedDigits.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 3)
        {
            return string.Empty;
        }

        var year = parts[0].PadLeft(4, '0');
        var month = parts[1].PadLeft(2, '0');
        var day = parts[2].PadLeft(2, '0');

        return string.Create(10, (year, month, day), static (span, state) =>
        {
            var (y, m, d) = state;
            y.AsSpan().CopyTo(span);
            span[4] = '-';
            m.AsSpan().CopyTo(span[5..]);
            span[7] = '-';
            d.AsSpan().CopyTo(span[8..]);
        });
    }

    private static bool TryExtractPersianDateParts(string value, out int year, out int month, out int day)
    {
        year = 0;
        month = 0;
        day = 0;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var parts = value.Split('-', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 3)
        {
            return false;
        }

        if (!int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out year))
        {
            return false;
        }

        if (!int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out month))
        {
            return false;
        }

        if (!int.TryParse(parts[2], NumberStyles.None, CultureInfo.InvariantCulture, out day))
        {
            return false;
        }

        return year > 0 && month is >= 1 and <= 12 && day is >= 1 and <= 31;
    }

    private static bool TryNormalizeTimeInput(
        string? value,
        out int hour,
        out int minute,
        out string normalizedValue,
        out string? errorMessage)
    {
        hour = 0;
        minute = 0;
        normalizedValue = string.Empty;
        errorMessage = null;

        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        var sanitized = NormalizeDigits(value)
            .Replace("\u200f", string.Empty, StringComparison.Ordinal)
            .Replace("\u200e", string.Empty, StringComparison.Ordinal)
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .Replace(".", ":", StringComparison.Ordinal)
            .Replace("-", ":", StringComparison.Ordinal)
            .Trim();

        if (sanitized.Length is 3 or 4 && !sanitized.Contains(':', StringComparison.Ordinal))
        {
            var insertIndex = sanitized.Length - 2;
            sanitized = sanitized.Insert(insertIndex, ":");
        }

        if (!TimeSpan.TryParseExact(
                sanitized,
                new[] { "hh\\:mm", "h\\:mm", "HH\\:mm", "H\\:mm" },
                CultureInfo.InvariantCulture,
                out var timeSpan))
        {
            errorMessage = "ساعت انتشار وارد شده معتبر نیست.";
            return false;
        }

        if (timeSpan.TotalHours >= 24)
        {
            errorMessage = "ساعت انتشار وارد شده معتبر نیست.";
            return false;
        }

        hour = timeSpan.Hours;
        minute = timeSpan.Minutes;
        normalizedValue = string.Format(CultureInfo.InvariantCulture, "{0:00}:{1:00}", hour, minute);

        return true;
    }

    private static string NormalizeDigits(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(value.Length);
        foreach (var ch in value)
        {
            builder.Append(ch switch
            {
                '۰' => '0',
                '۱' => '1',
                '۲' => '2',
                '۳' => '3',
                '۴' => '4',
                '۵' => '5',
                '۶' => '6',
                '۷' => '7',
                '۸' => '8',
                '۹' => '9',
                '٠' => '0',
                '١' => '1',
                '٢' => '2',
                '٣' => '3',
                '٤' => '4',
                '٥' => '5',
                '٦' => '6',
                '٧' => '7',
                '٨' => '8',
                '٩' => '9',
                _ => ch
            });
        }

        return builder.ToString();
    }

    private static TimeSpan GetIranOffset(DateTime dateTime)
    {
        foreach (var timeZoneId in new[] { "Iran Standard Time", "Asia/Tehran" })
        {
            try
            {
                var timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
                return timeZone.GetUtcOffset(dateTime);
            }
            catch (TimeZoneNotFoundException)
            {
                // Try next identifier.
            }
            catch (InvalidTimeZoneException)
            {
                // Try next identifier.
            }
        }

        return TimeSpan.FromHours(3.5);
    }

    private static IReadOnlyCollection<SelectListItem> BuildCategoryOptions(
        IReadOnlyCollection<SiteCategoryDto> categories,
        Guid? selectedId,
        bool includeAllOption,
        Guid? excludedId = null)
    {
        var items = new List<SelectListItem>();
        if (includeAllOption)
        {
            items.Add(new SelectListItem("همه دسته‌ها", string.Empty, selectedId is null));
        }

        foreach (var option in FlattenCategories(categories))
        {
            if (excludedId is not null && option.Id == excludedId)
            {
                continue;
            }

            items.Add(new SelectListItem(BuildIndentedLabel(option.Name, option.Depth), option.Id.ToString(), selectedId == option.Id));
        }

        return items;
    }

    private static IReadOnlyCollection<SelectListItem> BuildTypeOptions(ProductType? selectedType)
    {
        return new[]
        {
            new SelectListItem("همه نوع‌ها", string.Empty, selectedType is null),
            new SelectListItem("محصول فیزیکی", ProductType.Physical.ToString(), selectedType == ProductType.Physical),
            new SelectListItem("محصول دانلودی", ProductType.Digital.ToString(), selectedType == ProductType.Digital)
        };
    }

    private static IReadOnlyCollection<SelectListItem> BuildStatusOptions(bool? isPublished)
    {
        return new[]
        {
            new SelectListItem("همه وضعیت‌ها", string.Empty, isPublished is null),
            new SelectListItem("منتشر شده", bool.TrueString, isPublished == true),
            new SelectListItem("پیش‌نویس", bool.FalseString, isPublished == false)
        };
    }

    private async Task<ProductCategoriesViewModel> BuildCategoriesViewModelAsync(
        CancellationToken cancellationToken,
        ProductCategoryFormModel? createModel = null,
        ProductCategoryUpdateFormModel? editModel = null,
        Guid? highlightId = null)
    {
        var lookupsResult = await _mediator.Send(new GetProductLookupsQuery(), cancellationToken);
        if (!lookupsResult.IsSuccess && !string.IsNullOrWhiteSpace(lookupsResult.Error))
        {
            TempData["Error"] = lookupsResult.Error;
        }

        var lookups = lookupsResult.IsSuccess && lookupsResult.Value is not null
            ? lookupsResult.Value
            : new ProductLookupsDto(Array.Empty<SiteCategoryDto>(), Array.Empty<string>());

        var productCategories = (lookups.Categories ?? Array.Empty<SiteCategoryDto>())
            .Select(MapCategoryTree)
            .OrderBy(category => category.Name)
            .ToArray();

        var flatCategories = FlattenCategories(productCategories);

        var statistics = new ProductCategoryStatisticsViewModel(
            flatCategories.Count,
            flatCategories.Count(item => item.Depth == 0),
            flatCategories.Count(item => item.ChildCount > 0),
            flatCategories.Count(item => item.ChildCount == 0),
            flatCategories.Count == 0 ? 0 : flatCategories.Max(item => item.Depth));

        var createParentOptions = BuildParentOptions(flatCategories, createModel?.ParentId, excludedId: null);
        var editParentOptions = BuildParentOptions(flatCategories, editModel?.ParentId, excludedId: editModel?.Id);

        return new ProductCategoriesViewModel
        {
            Tree = productCategories,
            Categories = flatCategories,
            Statistics = statistics,
            CreateCategory = createModel ?? new ProductCategoryFormModel(),
            EditCategory = editModel ?? new ProductCategoryUpdateFormModel(),
            CreateParentOptions = createParentOptions,
            EditParentOptions = editParentOptions,
            HighlightedCategoryId = highlightId
        };
    }

    private static ProductCategoryTreeItemViewModel MapCategoryTree(SiteCategoryDto category)
    {
        var children = (category.Children ?? Array.Empty<SiteCategoryDto>())
            .Select(MapCategoryTree)
            .OrderBy(child => child.Name)
            .ToArray();

        var descendantIds = children
            .SelectMany(child => child.DescendantIds.Prepend(child.Id))
            .Distinct()
            .ToArray();

        return new ProductCategoryTreeItemViewModel
        {
            Id = category.Id,
            Name = category.Name,
            Slug = category.Slug,
            Description = category.Description,
            ParentId = category.ParentId,
            Depth = category.Depth,
            Children = children,
            DescendantIds = descendantIds
        };
    }

    private static List<ProductCategoryFlatItemViewModel> FlattenCategories(IEnumerable<ProductCategoryTreeItemViewModel> categories)
    {
        var result = new List<ProductCategoryFlatItemViewModel>();

        foreach (var category in categories)
        {
            AppendCategory(category, null, result);
        }

        return result;
    }

    private static void AppendCategory(
        ProductCategoryTreeItemViewModel category,
        string? parentName,
        ICollection<ProductCategoryFlatItemViewModel> destination)
    {
        var children = category.Children?.ToArray() ?? Array.Empty<ProductCategoryTreeItemViewModel>();

        destination.Add(new ProductCategoryFlatItemViewModel(
            category.Id,
            category.Name,
            category.Slug,
            category.Description,
            category.ParentId,
            parentName,
            category.Depth,
            children.Length,
            category.DescendantIds?.Count ?? 0));

        foreach (var child in children)
        {
            AppendCategory(child, category.Name, destination);
        }
    }

    private static IReadOnlyCollection<SelectListItem> BuildParentOptions(
        IReadOnlyCollection<ProductCategoryFlatItemViewModel> categories,
        Guid? selectedId,
        Guid? excludedId)
    {
        var options = new List<SelectListItem>
        {
            new("بدون والد", string.Empty, selectedId is null)
        };

        foreach (var category in categories)
        {
            if (excludedId is not null && category.Id == excludedId)
            {
                continue;
            }

            options.Add(new SelectListItem(
                BuildIndentedLabel(category.Name, category.Depth),
                category.Id.ToString(),
                selectedId == category.Id));
        }

        return options;
    }

    private static string BuildIndentedLabel(string text, int depth)
    {
        if (depth <= 0)
        {
            return text;
        }

        var indent = string.Concat(Enumerable.Repeat("⎯ ", depth));
        return $"{indent}{text}";
    }

    private static IReadOnlyCollection<ProductCategoryFlatItemViewModel> FlattenCategories(IReadOnlyCollection<SiteCategoryDto> categories)
    {
        var tree = categories.Select(MapCategoryTree).OrderBy(category => category.Name).ToArray();
        return FlattenCategories(tree);
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using TestAttarClone.Application.Commands.Catalog;
using TestAttarClone.Application.DTOs.Catalog;
using TestAttarClone.Application.Interfaces;
using TestAttarClone.Application.Queries.Catalog;
using TestAttarClone.Domain.Enums;
using TestAttarClone.SharedKernel.Authorization;
using TestAttarClone.SharedKernel.Extensions;
using TestAttarClone.WebSite.Areas.Seller.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace TestAttarClone.WebSite.Areas.Seller.Controllers;

[Area("Seller")]
[Authorize(Policy = AuthorizationPolicies.SellerPanelAccess)]
public sealed class ProductsController : Controller
{
    private const string FeaturedUploadFolder = "products/featured";
    private const int MaxImageSizeKb = 200;

    private static readonly HashSet<string> AllowedImageContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/png",
        "image/jpeg",
        "image/webp",
        "image/gif"
    };

    private const string ProductsTabKey = "products";
    private const string CreateTabKey = "create";
    private const string EditTabKey = "edit";

    private readonly IMediator _mediator;
    private readonly IFormFileSettingServices _fileSettingServices;

    public ProductsController(IMediator mediator, IFormFileSettingServices fileSettingServices)
    {
        _mediator = mediator;
        _fileSettingServices = fileSettingServices;
    }

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] SellerProductFilterRequest? filters)
    {
        ConfigureLayoutContext(ProductsTabKey);
        ViewData["Title"] = "محصولات من";
        ViewData["Subtitle"] = "محصولات ثبت‌شده برای تایید مدیریت";

        var filter = filters ?? new SellerProductFilterRequest();
        var searchTerm = string.IsNullOrWhiteSpace(filter.SearchTerm) ? null : filter.SearchTerm.Trim();
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        var result = await _mediator.Send(new GetSellerProductsQuery(userId));

        IReadOnlyCollection<ProductListItemDto> productDtos;
        string? errorMessage = null;

        if (!result.IsSuccess || result.Value is null)
        {
            productDtos = Array.Empty<ProductListItemDto>();
            errorMessage = result.Error ?? "دریافت لیست محصولات با خطا مواجه شد.";
        }
        else
        {
            productDtos = result.Value;
        }

        var filtered = ApplyFilters(productDtos, filter)
            .Select(MapToViewModel)
            .ToArray();

        var viewModel = new SellerProductIndexViewModel
        {
            Products = filtered,
            TotalCount = filtered.Length,
            PublishedCount = filtered.Count(product => product.IsPublished),
            PendingCount = filtered.Count(product => !product.IsPublished),
            SuccessMessage = TempData["Seller.Success"] as string,
            ErrorMessage = errorMessage ?? TempData["Seller.Error"] as string,
            Filter = new SellerProductFilterViewModel
            {
                SearchTerm = searchTerm,
                Type = filter.Type,
                Status = filter.Status
            }
        };

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id)
    {
        ConfigureLayoutContext(ProductsTabKey);
        ViewData["Title"] = "جزئیات محصول";
        ViewData["Subtitle"] = "مرور اطلاعات و آمار فروش";

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        var cancellationToken = HttpContext.RequestAborted;

        var detailResult = await _mediator.Send(new GetSellerProductDetailQuery(id, userId), cancellationToken);
        if (!detailResult.IsSuccess || detailResult.Value is null)
        {
            TempData["Seller.Error"] = detailResult.Error ?? "دسترسی به محصول امکان‌پذیر نیست.";
            return RedirectToAction(nameof(Index));
        }

        var product = detailResult.Value;

        var salesResult = await _mediator.Send(new GetProductSalesSummaryQuery(id), cancellationToken);
        var salesViewModel = salesResult.IsSuccess && salesResult.Value is not null
            ? MapSalesToViewModel(salesResult.Value)
            : SellerProductSalesSummaryViewModel.Empty;

        var tags = ParseTags(product.TagList);
        var gallery = product.Gallery
            .OrderBy(image => image.DisplayOrder)
            .ThenBy(image => image.Id)
            .Select(image => new SellerProductGalleryItemViewModel(image.Id, image.ImagePath, image.DisplayOrder))
            .ToArray();

        var viewModel = new SellerProductDetailViewModel
        {
            Id = product.Id,
            Name = product.Name,
            Summary = product.Summary,
            Description = product.Description,
            Type = product.Type,
            Price = product.Price,
            CompareAtPrice = product.CompareAtPrice,
            TrackInventory = product.TrackInventory,
            StockQuantity = product.StockQuantity,
            IsPublished = product.IsPublished,
            PublishedAt = product.PublishedAt,
            CreatedAt = product.CreatedAt,
            UpdatedAt = product.UpdatedAt,
            CategoryName = product.CategoryName,
            FeaturedImagePath = product.FeaturedImagePath,
            DigitalDownloadPath = product.DigitalDownloadPath,
            Tags = tags,
            Gallery = gallery,
            Sales = salesViewModel,
            ViewCount = product.ViewCount
        };

        ViewData["Title"] = $"جزئیات محصول - {product.Name}";

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        ConfigureLayoutContext(CreateTabKey);
        ViewData["Title"] = "درخواست افزودن محصول";
        ViewData["Subtitle"] = "پس از تایید ادمین در سایت منتشر می‌شود";

        var model = new SellerProductFormViewModel
        {
            TagItems = Array.Empty<string>()
        };
        await PopulateFormOptionsAsync(model, HttpContext.RequestAborted);

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> SearchProducts(string term, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(term) || term.Length < 2)
        {
            return Json(Array.Empty<object>());
        }

        var filter = new ProductListFilterDto(
            Page: 1,
            PageSize: 20,
            SearchTerm: term.Trim(),
            Type: null,
            IsPublished: true,
            MinPrice: null,
            MaxPrice: null,
            SellerId: null);

        var repository = HttpContext.RequestServices.GetRequiredService<IProductRepository>();
        var result = await repository.GetListAsync(filter, null, cancellationToken);

        var products = result.Items
            .Where(p => p.IsPublished)
            .Select(p => new
            {
                id = p.Id,
                name = p.Name
            })
            .ToArray();

        return Json(products);
    }

    [HttpGet]
    public async Task<IActionResult> GetProductVariants(Guid id, CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
        {
            return NotFound();
        }

        var result = await _mediator.Send(new GetProductDetailQuery(id), cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return NotFound();
        }

        var product = result.Value;
        return Json(new
        {
            hasVariants = product.VariantAttributes.Any() && product.Variants.Any(),
            variantAttributes = product.VariantAttributes.Select(attr => new
            {
                id = attr.Id,
                name = attr.Name,
                options = attr.Options,
                displayOrder = attr.DisplayOrder
            }),
            variants = product.Variants.Select(v => new
            {
                id = v.Id,
                price = v.Price,
                compareAtPrice = v.CompareAtPrice,
                stockQuantity = v.StockQuantity,
                sku = v.Sku,
                isActive = v.IsActive,
                options = v.Options.Select(opt => new
                {
                    variantAttributeId = opt.VariantAttributeId,
                    value = opt.Value
                })
            })
        });
    }

    [HttpGet]
    public async Task<IActionResult> GetProductInfo(Guid id, CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
        {
            return NotFound();
        }

        var repository = HttpContext.RequestServices.GetRequiredService<IProductRepository>();
        var product = await repository.GetByIdAsync(id, cancellationToken);
        if (product is null || product.IsDeleted)
        {
            return NotFound();
        }

        return Json(new
        {
            id = product.Id,
            name = product.Name,
            slug = product.SeoSlug,
            price = product.Price,
            type = product.Type.ToString()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SellerProductFormViewModel model)
    {
        ConfigureLayoutContext(CreateTabKey);
        ViewData["Title"] = "درخواست افزودن محصول";
        ViewData["Subtitle"] = "پس از تایید ادمین در سایت منتشر می‌شود";

        var cancellationToken = HttpContext.RequestAborted;

        var tagItems = ParseTags(model.Tags);
        model.TagItems = tagItems;
        model.Tags = string.Join(", ", tagItems);
        model.FeaturedImagePath = string.IsNullOrWhiteSpace(model.FeaturedImagePath)
            ? null
            : model.FeaturedImagePath.Trim();

        // Validate based on request type
        if (model.RequestType == ProductRequestType.OfferForExistingProduct)
        {
            if (!model.ExistingProductId.HasValue || model.ExistingProductId.Value == Guid.Empty)
            {
                ModelState.AddModelError(nameof(model.ExistingProductId), "برای ثبت پیشنهاد، محصول موجود را انتخاب کنید.");
            }
            // For offers, we don't need category, name, description, etc. - they're from the existing product
            ModelState.Remove(nameof(model.CategoryId));
            ModelState.Remove(nameof(model.Name));
            ModelState.Remove(nameof(model.Summary));
            ModelState.Remove(nameof(model.Description));
            ModelState.Remove(nameof(model.Type));
            ModelState.Remove(nameof(model.FeaturedImageUpload));
            ModelState.Remove(nameof(model.FeaturedImagePath));
            ModelState.Remove(nameof(model.Tags));
            ModelState.Remove(nameof(model.Brand));
        }
        else
        {
            // Validate new product fields
            if (model.CategoryId is null || model.CategoryId == Guid.Empty)
            {
                ModelState.AddModelError(nameof(model.CategoryId), "دسته‌بندی محصول را انتخاب کنید.");
            }

            if (model.Type == ProductType.Digital)
            {
                model.TrackInventory = false;
                model.StockQuantity = 0;

                if (string.IsNullOrWhiteSpace(model.DigitalDownloadPath))
                {
                    ModelState.AddModelError(nameof(model.DigitalDownloadPath), "برای محصولات دانلودی وارد کردن لینک فایل الزامی است.");
                }
            }
            else if (!model.TrackInventory)
            {
                model.StockQuantity = 0;
            }

            ValidateFeaturedImage(model.FeaturedImageUpload, nameof(model.FeaturedImageUpload));
        }

        if (!ModelState.IsValid)
        {
            await PopulateFormOptionsAsync(model, cancellationToken);
            return View(model);
        }

        string? uploadedImagePath = null;
        if (model.RequestType == ProductRequestType.NewProduct)
        {
            uploadedImagePath = await SaveFeaturedImageAsync(model.FeaturedImageUpload, nameof(model.FeaturedImageUpload));

            if (!ModelState.IsValid)
            {
                await PopulateFormOptionsAsync(model, cancellationToken);
                return View(model);
            }

            if (!string.IsNullOrWhiteSpace(uploadedImagePath))
            {
                model.FeaturedImagePath = uploadedImagePath;
                model.FeaturedImageUpload = null;
            }
        }

        // Check if it's an offer for existing product or new product request
        if (model.RequestType == ProductRequestType.OfferForExistingProduct)
        {
            if (!model.ExistingProductId.HasValue || model.ExistingProductId.Value == Guid.Empty)
            {
                ModelState.AddModelError(nameof(model.ExistingProductId), "برای ثبت پیشنهاد، محصول موجود را انتخاب کنید.");
                await PopulateFormOptionsAsync(model, cancellationToken);
                return View(model);
            }

            // Submit offer for existing product
            var offerCommand = new SubmitSellerProductOfferCommand(
                model.ExistingProductId.Value,
                model.Price,
                model.TrackInventory,
                model.TrackInventory ? model.StockQuantity : 0,
                model.DigitalDownloadPath);

            var offerResult = await _mediator.Send(offerCommand, cancellationToken);
            if (!offerResult.IsSuccess)
            {
                ModelState.AddModelError(string.Empty, offerResult.Error ?? "ثبت پیشنهاد محصول با خطا مواجه شد.");
                await PopulateFormOptionsAsync(model, cancellationToken);
                return View(model);
            }

            TempData["Success"] = "پیشنهاد شما برای محصول ثبت شد. پس از تایید مدیریت در سایت نمایش داده می‌شود.";
            return RedirectToAction("Index", "ProductRequests");
        }
        else
        {
            // Submit new product request
            var command = new SubmitSellerProductCommand(
                model.Name,
                model.Summary,
                model.Description,
                model.Type,
                model.Price,
                model.TrackInventory,
                model.TrackInventory ? model.StockQuantity : 0,
                model.CategoryId!.Value,
                model.Tags,
                model.FeaturedImagePath,
                model.Type == ProductType.Digital ? model.DigitalDownloadPath : null,
                model.Brand);

            var result = await _mediator.Send(command, cancellationToken);
            if (!result.IsSuccess)
            {
                ModelState.AddModelError(string.Empty, result.Error ?? "ثبت درخواست محصول با خطا مواجه شد.");
                await PopulateFormOptionsAsync(model, cancellationToken);
                return View(model);
            }

            TempData["Success"] = "درخواست افزودن محصول شما ثبت شد. پس از تایید مدیریت در سایت نمایش داده می‌شود.";
            return RedirectToAction("Index", "ProductRequests");
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        ConfigureLayoutContext(EditTabKey);
        ViewData["Title"] = "ویرایش محصول";
        ViewData["Subtitle"] = "تغییرات پس از تایید مجدد مدیریت اعمال می‌شود";

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        var cancellationToken = HttpContext.RequestAborted;
        var result = await _mediator.Send(new GetSellerProductDetailQuery(id, userId), cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            TempData["Teacher.Error"] = result.Error ?? "دسترسی به محصول امکان‌پذیر نیست.";
            return RedirectToAction(nameof(Index));
        }

        var dto = result.Value;
        var tags = ParseTags(dto.TagList);

        var model = new SellerProductFormViewModel
        {
            Id = dto.Id,
            Name = dto.Name,
            Summary = string.IsNullOrWhiteSpace(dto.Summary) ? null : dto.Summary,
            Description = dto.Description,
            Type = dto.Type,
            Price = dto.Price,
            TrackInventory = dto.TrackInventory,
            StockQuantity = dto.StockQuantity,
            CategoryId = dto.CategoryId,
            Tags = string.Join(", ", tags),
            TagItems = tags,
            FeaturedImagePath = dto.FeaturedImagePath,
            DigitalDownloadPath = dto.DigitalDownloadPath,
            Brand = dto.Brand,
            WasPreviouslyPublished = dto.IsPublished
        };

        if (model.Type == ProductType.Digital)
        {
            model.TrackInventory = false;
            model.StockQuantity = 0;
        }

        await PopulateFormOptionsAsync(model, cancellationToken);

        return View("Edit", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, SellerProductFormViewModel model)
    {
        ConfigureLayoutContext(EditTabKey);
        ViewData["Title"] = "ویرایش محصول";
        ViewData["Subtitle"] = "تغییرات پس از تایید مجدد مدیریت اعمال می‌شود";

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        model.Id = id;
        var cancellationToken = HttpContext.RequestAborted;

        var tags = ParseTags(model.Tags);
        model.TagItems = tags;
        model.Tags = string.Join(", ", tags);
        model.FeaturedImagePath = string.IsNullOrWhiteSpace(model.FeaturedImagePath)
            ? null
            : model.FeaturedImagePath.Trim();

        if (model.CategoryId is null || model.CategoryId == Guid.Empty)
        {
            ModelState.AddModelError(nameof(model.CategoryId), "دسته‌بندی محصول را انتخاب کنید.");
        }

        if (model.Type == ProductType.Digital)
        {
            model.TrackInventory = false;
            model.StockQuantity = 0;

            if (string.IsNullOrWhiteSpace(model.DigitalDownloadPath))
            {
                ModelState.AddModelError(nameof(model.DigitalDownloadPath), "برای محصولات دانلودی وارد کردن لینک فایل الزامی است.");
            }
        }
        else if (!model.TrackInventory)
        {
            model.StockQuantity = 0;
        }

        ValidateFeaturedImage(model.FeaturedImageUpload, nameof(model.FeaturedImageUpload));

        if (!ModelState.IsValid)
        {
            await PopulateFormOptionsAsync(model, cancellationToken);
            return View(model);
        }

        var uploadedImagePath = await SaveFeaturedImageAsync(model.FeaturedImageUpload, nameof(model.FeaturedImageUpload));

        if (!ModelState.IsValid)
        {
            await PopulateFormOptionsAsync(model, cancellationToken);
            return View(model);
        }

        if (!string.IsNullOrWhiteSpace(uploadedImagePath))
        {
            model.FeaturedImagePath = uploadedImagePath;
            model.FeaturedImageUpload = null;
        }

        var command = new UpdateSellerProductCommand(
            id,
            userId,
            model.Name,
            model.Summary,
            model.Description,
            model.Type,
            model.Price,
            model.TrackInventory,
            model.TrackInventory ? model.StockQuantity : 0,
            model.CategoryId!.Value,
            model.Tags,
            model.FeaturedImagePath,
            model.Type == ProductType.Digital ? model.DigitalDownloadPath : null,
            model.Brand);

        var result = await _mediator.Send(command, cancellationToken);
        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "ویرایش محصول با خطا مواجه شد.");
            await PopulateFormOptionsAsync(model, cancellationToken);
            return View(model);
        }

        var successMessage = result.Value
            ? "محصول ویرایش شد و تا تایید مجدد ادمین در حالت انتظار قرار گرفت."
            : "تغییرات محصول برای بررسی ارسال شد.";

        TempData["Seller.Success"] = successMessage;
        TempData["Alert.Message"] = successMessage;
        TempData["Alert.Type"] = "success";
        TempData["Alert.Title"] = "ویرایش ثبت شد";

        return RedirectToAction(nameof(Index));
    }

    private void ValidateFeaturedImage(IFormFile? file, string fieldName)
    {
        if (file is null || file.Length == 0)
        {
            return;
        }

        if (!_fileSettingServices.IsFileSizeValid(file, MaxImageSizeKb))
        {
            ModelState.AddModelError(fieldName, $"حجم تصویر نباید بیش از {MaxImageSizeKb} کیلوبایت باشد.");
            return;
        }

        if (string.IsNullOrWhiteSpace(file.ContentType) || !AllowedImageContentTypes.Contains(file.ContentType))
        {
            ModelState.AddModelError(fieldName, "تنها فرمت‌های png، jpg، webp یا gif مجاز است.");
        }
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

    private void ConfigureLayoutContext(string activeTab)
    {
        var fullName = User?.FindFirstValue("FullName") ?? User?.Identity?.Name ?? "مدرس";
        var initial = !string.IsNullOrWhiteSpace(fullName) ? fullName.Trim()[0].ToString() : "م";
        var email = User?.FindFirstValue(ClaimTypes.Email);
        var phone = User?.FindFirstValue(ClaimTypes.MobilePhone) ?? User?.FindFirstValue("PhoneNumber");

        ViewData["TitleSuffix"] = "پنل مدرس";
        ViewData["GreetingTitle"] = $"سلام، {fullName} 👋";
        ViewData["GreetingSubtitle"] = "مدیریت درخواست‌های دوره و محصول";
        ViewData["GreetingInitial"] = initial;
        ViewData["AccountName"] = fullName;
        ViewData["AccountInitial"] = initial;
        if (!string.IsNullOrWhiteSpace(email))
        {
            ViewData["AccountEmail"] = email;
        }

        if (!string.IsNullOrWhiteSpace(phone))
        {
            ViewData["AccountPhone"] = phone;
        }

        ViewData["SearchPlaceholder"] = "جستجو در محصولات مدرس";
        ViewData["ShowSearch"] = false;
        ViewData["Sidebar:ActiveTab"] = activeTab;
    }

    private static SellerProductListItemViewModel MapToViewModel(ProductListItemDto dto)
    {
        var tags = string.IsNullOrWhiteSpace(dto.TagList)
            ? Array.Empty<string>()
            : dto.TagList.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return new SellerProductListItemViewModel(
            dto.Id,
            dto.Name,
            dto.CategoryName,
            dto.Type,
            dto.Price,
            dto.IsPublished,
            dto.PublishedAt,
            dto.UpdatedAt,
            dto.FeaturedImagePath,
            tags);
    }

    private static IEnumerable<ProductListItemDto> ApplyFilters(
        IReadOnlyCollection<ProductListItemDto> products,
        SellerProductFilterRequest filter)
    {
        if (products.Count == 0)
        {
            return Array.Empty<ProductListItemDto>();
        }

        var query = products.AsEnumerable();

        var searchTerm = filter.SearchTerm?.Trim();
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(product =>
                product.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
                || product.CategoryName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
        }

        if (filter.Type.HasValue)
        {
            var type = filter.Type.Value;
            query = query.Where(product => product.Type == type);
        }

        if (filter.Status.HasValue)
        {
            var isPublished = filter.Status.Value == SellerProductStatusFilter.Published;
            query = query.Where(product => product.IsPublished == isPublished);
        }

        return query;
    }

    private async Task PopulateFormOptionsAsync(SellerProductFormViewModel model, CancellationToken cancellationToken)
    {
        var lookupsResult = await _mediator.Send(new GetProductLookupsQuery(), cancellationToken);
        if (lookupsResult.IsSuccess && lookupsResult.Value is not null)
        {
            model.CategoryOptions = BuildCategoryOptions(lookupsResult.Value.Categories, model.CategoryId);
            model.TagSuggestions = lookupsResult.Value.Tags;
        }
        else
        {
            model.CategoryOptions = Array.Empty<SelectListItem>();
            model.TagSuggestions = Array.Empty<string>();
        }

        model.TypeOptions = BuildTypeOptions(model.Type);
    }

    private static IReadOnlyCollection<SelectListItem> BuildCategoryOptions(
        IReadOnlyCollection<SiteCategoryDto> categories,
        Guid? selectedCategory)
    {
        if (categories.Count == 0)
        {
            return Array.Empty<SelectListItem>();
        }

        var items = new List<SelectListItem>();

        void Traverse(IEnumerable<SiteCategoryDto> nodes, int depth)
        {
            foreach (var node in nodes.OrderBy(node => node.Name))
            {
                var prefix = depth > 0 ? string.Concat(Enumerable.Repeat("— ", depth)) : string.Empty;
                var text = string.Concat(prefix, node.Name);
                items.Add(new SelectListItem(text, node.Id.ToString(), selectedCategory.HasValue && node.Id == selectedCategory.Value));

                if (node.Children.Count > 0)
                {
                    Traverse(node.Children, depth + 1);
                }
            }
        }

        Traverse(categories, 0);
        return items;
    }

    private static IReadOnlyCollection<SelectListItem> BuildTypeOptions(ProductType selectedType)
        => Enum.GetValues<ProductType>()
            .Select(type => new SelectListItem(GetProductTypeTitle(type), ((int)type).ToString(), type == selectedType))
            .ToArray();

    private static string GetProductTypeTitle(ProductType type)
        => type switch
        {
            ProductType.Digital => "محصول دانلودی",
            ProductType.Physical => "محصول فیزیکی",
            _ => type.ToString()
        };

    private static IReadOnlyCollection<string> ParseTags(string? rawTags)
    {
        if (string.IsNullOrWhiteSpace(rawTags))
        {
            return Array.Empty<string>();
        }

        var separators = new[] { ',', '،', ';', '|', '\n', '\r' };

        return rawTags
            .Split(separators, StringSplitOptions.RemoveEmptyEntries)
            .Select(tag => tag.Trim())
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Select(tag => tag.Length > 50 ? tag[..50] : tag)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static SellerProductSalesSummaryViewModel MapSalesToViewModel(ProductSalesSummaryDto dto)
    {
        var trend = dto.Trend
            .OrderBy(point => point.PeriodStart)
            .Select(point => new SellerProductSalesTrendPointViewModel(
                point.PeriodStart.ToPersianDateString(),
                point.Quantity,
                point.Revenue))
            .ToArray();

        return new SellerProductSalesSummaryViewModel(
            dto.TotalOrders,
            dto.TotalQuantity,
            dto.TotalRevenue,
            dto.TotalDiscount,
            dto.AverageOrderValue,
            dto.FirstSaleAt,
            dto.LastSaleAt,
            trend);
    }
}

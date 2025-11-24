using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EndPoint.WebSite.Models.Product;
using EndPoint.WebSite.Services.Products;
using Microsoft.AspNetCore.Mvc;

namespace EndPoint.WebSite.Controllers;

public class ProductController : Controller
{
    private static readonly IReadOnlyList<(string Value, double Threshold, string Label)> RatingOptions = new List<(string, double, string)>
    {
        ("4.5", 4.5, "امتیاز ۴.۵ به بالا"),
        ("4", 4.0, "امتیاز ۴ به بالا"),
        ("3.5", 3.5, "امتیاز ۳.۵ به بالا")
    };

    private static readonly IReadOnlyList<(string Value, string Label)> SortOptions = new List<(string, string)>
    {
        ("newest", "جدیدترین"),
        ("price-asc", "ارزان‌ترین"),
        ("price-desc", "گران‌ترین"),
        ("rating", "بیشترین امتیاز")
    };

    private readonly IProductCatalogService _productCatalogService;

    public ProductController(IProductCatalogService productCatalogService)
    {
        _productCatalogService = productCatalogService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? search,
        string? category,
        string? format,
        decimal? minPrice,
        decimal? maxPrice,
        double? rating,
        string? sort,
        CancellationToken cancellationToken)
    {
        var selectedSort = string.IsNullOrWhiteSpace(sort) ? "newest" : sort;
        var filterOptions = new ProductFilterOptions(
            search,
            category,
            format,
            minPrice,
            maxPrice,
            rating,
            selectedSort);

        var result = await _productCatalogService.GetProductsAsync(filterOptions, cancellationToken);
        var summaries = result.Products.Select(MapToSummary).ToList();

        var filters = new ProductFilterViewModel
        {
            SearchTerm = search,
            SelectedCategory = category,
            SelectedDeliveryFormat = format,
            SelectedSort = selectedSort,
            MinPrice = minPrice,
            MaxPrice = maxPrice,
            MinRating = rating,
            PriceRangeMin = result.PriceRangeMin,
            PriceRangeMax = result.PriceRangeMax,
            Categories = result.Categories
                .Select(value => new ProductFilterOptionViewModel
                {
                    Value = value,
                    Label = value,
                    Count = result.Products.Count(product => string.Equals(product.Category, value, StringComparison.OrdinalIgnoreCase)),
                    IsSelected = string.Equals(value, category, StringComparison.OrdinalIgnoreCase)
                })
                .ToList(),
            DeliveryFormats = result.DeliveryFormats
                .Select(value => new ProductFilterOptionViewModel
                {
                    Value = value,
                    Label = value,
                    Count = result.Products.Count(product => string.Equals(product.DeliveryFormat, value, StringComparison.OrdinalIgnoreCase)),
                    IsSelected = string.Equals(value, format, StringComparison.OrdinalIgnoreCase)
                })
                .ToList(),
            RatingOptions = RatingOptions
                .Select(option => new ProductFilterOptionViewModel
                {
                    Value = option.Value,
                    Label = option.Label,
                    Count = result.Products.Count(product => product.Rating >= option.Threshold),
                    IsSelected = rating.HasValue && Math.Abs(rating.Value - option.Threshold) < 0.01
                })
                .ToList(),
            SortOptions = SortOptions
                .Select(option => new ProductFilterOptionViewModel
                {
                    Value = option.Value,
                    Label = option.Label,
                    Count = 0,
                    IsSelected = string.Equals(option.Value, selectedSort, StringComparison.OrdinalIgnoreCase)
                })
                .ToList()
        };

        var viewModel = new ProductListViewModel
        {
            Products = summaries,
            Filters = filters,
            TotalCount = result.TotalCount
        };

        ViewData["Title"] = "محصولات تخصصی آرسیس";
        ViewData["MetaDescription"] = "پکیج‌ها و محصولات تخصصی آرسیس برای تحلیل استعداد، توسعه رهبری و استقرار تیم‌های چابک.";
        ViewData["CanonicalUrl"] = Url.Action("Index", "Product", null, Request.Scheme);

        return View(viewModel);
    }

    [HttpGet("/product/{slug}")]
    public async Task<IActionResult> Details(string slug, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return NotFound();
        }

        var product = await _productCatalogService.GetBySlugAsync(slug, cancellationToken);
        if (product is null)
        {
            return NotFound();
        }

        var detailViewModel = await BuildDetailViewModelAsync(product, cancellationToken);

        ViewData["Title"] = product.Name;
        ViewData["MetaDescription"] = product.ShortDescription;
        ViewData["MetaOgImage"] = product.HeroImageUrl;
        ViewData["MetaOgType"] = "product";
        ViewData["MetaOgUrl"] = Url.Action("Details", "Product", new { slug }, Request.Scheme);

        return View(detailViewModel);
    }

    [HttpPost("/product/{slug}/comments")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddComment(
        string slug,
        [Bind(Prefix = "NewComment")] ProductCommentFormModel form,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return NotFound();
        }

        var product = await _productCatalogService.GetBySlugAsync(slug.Trim(), cancellationToken);
        if (product is null)
        {
            return NotFound();
        }

        form ??= new ProductCommentFormModel();
        form.ProductId = product.Id;

        ViewData["Title"] = product.Name;
        ViewData["MetaDescription"] = product.ShortDescription;
        ViewData["MetaOgImage"] = product.HeroImageUrl;
        ViewData["MetaOgType"] = "product";
        ViewData["MetaOgUrl"] = Url.Action("Details", "Product", new { slug }, Request.Scheme);

        if (!ModelState.IsValid)
        {
            var invalidViewModel = await BuildDetailViewModelAsync(product, cancellationToken, form);
            return View("Details", invalidViewModel);
        }

        var success = await _productCatalogService.AddCommentAsync(
            product.Id,
            form.AuthorName,
            form.Content,
            form.Rating,
            form.ParentId,
            cancellationToken);

        if (!success)
        {
            ModelState.AddModelError(string.Empty, "ثبت نظر با مشکل مواجه شد. لطفاً دوباره تلاش کنید.");
            var failureViewModel = await BuildDetailViewModelAsync(product, cancellationToken, form);
            return View("Details", failureViewModel);
        }

        TempData["ProductCommentSuccess"] = true;
        var redirectUrl = Url.Action(nameof(Details), new { slug });
        if (string.IsNullOrWhiteSpace(redirectUrl))
        {
            return RedirectToAction(nameof(Details), new { slug });
        }

        return Redirect(redirectUrl + "#comments");
    }

    private async Task<ProductDetailViewModel> BuildDetailViewModelAsync(
        Models.Product.Product product,
        CancellationToken cancellationToken,
        ProductCommentFormModel? form = null)
    {
        var relatedProducts = await _productCatalogService.GetRelatedProductsAsync(product.Id, 4, cancellationToken);

        var commentForm = form ?? new ProductCommentFormModel { ProductId = product.Id };
        if (commentForm.ProductId == Guid.Empty)
        {
            commentForm.ProductId = product.Id;
        }

        return new ProductDetailViewModel
        {
            Product = MapToSummary(product),
            HeroImageUrl = product.HeroImageUrl,
            Description = product.Description,
            DifficultyLevel = product.DifficultyLevel,
            Duration = product.Duration,
            Highlights = product.Highlights,
            Modules = product.Modules.Select(module => new ProductModuleViewModel
            {
                Title = module.Title,
                Description = module.Description,
                Duration = module.Duration
            }).ToList(),
            Statistics = product.Statistics.Select(stat => new ProductStatisticViewModel
            {
                Label = stat.Label,
                Value = stat.Value,
                Tooltip = stat.Tooltip
            }).ToList(),
            FaqItems = product.FaqItems.Select(faq => new ProductFaqItemViewModel
            {
                Question = faq.Question,
                Answer = faq.Answer
            }).ToList(),
            Comments = BuildCommentTree(product.Comments),
            RelatedProducts = relatedProducts.Select(MapToSummary).ToList(),
            NewComment = commentForm
        };
    }

    private static ProductSummaryViewModel MapToSummary(Models.Product.Product product)
    {
        return new ProductSummaryViewModel
        {
            Id = product.Id,
            Slug = product.Slug,
            Name = product.Name,
            ShortDescription = product.ShortDescription,
            Price = product.Price,
            OriginalPrice = product.OriginalPrice,
            ThumbnailUrl = product.ThumbnailUrl,
            Category = product.Category,
            DeliveryFormat = product.DeliveryFormat,
            Rating = product.Rating,
            ReviewCount = product.ReviewCount,
            Badge = product.Duration,
            Tags = product.Tags
        };
    }

    private static IReadOnlyList<ProductCommentViewModel> BuildCommentTree(IReadOnlyList<ProductComment> comments)
    {
        if (comments.Count == 0)
        {
            return Array.Empty<ProductCommentViewModel>();
        }

        var grouped = comments
            .GroupBy(comment => comment.ParentId ?? Guid.Empty)
            .ToDictionary(group => group.Key, group => group.ToList());

        List<ProductCommentViewModel> BuildLevel(Guid parentId)
        {
            if (!grouped.TryGetValue(parentId, out var children))
            {
                return new List<ProductCommentViewModel>();
            }

            return children
                .OrderByDescending(comment => comment.CreatedAt)
                .Select(comment => new ProductCommentViewModel
                {
                    Id = comment.Id,
                    ParentId = comment.ParentId,
                    AuthorName = comment.AuthorName,
                    Content = comment.Content,
                    CreatedAt = comment.CreatedAt,
                    Rating = comment.Rating,
                    Replies = BuildLevel(comment.Id)
                })
                .ToList();
        }

        return BuildLevel(Guid.Empty);
    }
}

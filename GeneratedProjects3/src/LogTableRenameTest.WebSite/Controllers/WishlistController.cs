using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using LogTableRenameTest.Application.Commands.Catalog;
using LogTableRenameTest.Application.Queries.Catalog;
using LogTableRenameTest.WebSite.Models.Product;
using LogTableRenameTest.WebSite.Services.Products;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogTableRenameTest.WebSite.Controllers;

[Authorize]
public sealed class WishlistController : Controller
{
    private readonly IMediator _mediator;
    private readonly IProductCatalogService _productCatalogService;

    public WishlistController(IMediator mediator, IProductCatalogService productCatalogService)
    {
        _mediator = mediator;
        _productCatalogService = productCatalogService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        var result = await _mediator.Send(new GetWishlistQuery(userId), cancellationToken);
        
        if (!result.IsSuccess || result.Value is null)
        {
            ViewBag.ErrorMessage = result.Error ?? "خطا در دریافت لیست علاقه‌مندی‌ها";
            return View("Index", new WishlistViewModel { Products = Array.Empty<ProductSummaryViewModel>() });
        }

        var products = result.Value;
        var productSummaries = products.Select(MapToSummary).ToList();

        var viewModel = new WishlistViewModel
        {
            Products = productSummaries
        };

        ViewBag.SuccessMessage = TempData["Wishlist.Success"] as string;
        ViewBag.ErrorMessage = TempData["Wishlist.Error"] as string;

        return View("Index", viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(Guid productId, CancellationToken cancellationToken)
    {
        if (productId == Guid.Empty)
        {
            return Json(new { success = false, error = "محصول انتخاب شده معتبر نیست." });
        }

        var userId = GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Json(new { success = false, error = "لطفا ابتدا وارد حساب کاربری خود شوید." });
        }

        var result = await _mediator.Send(new ToggleWishlistCommand(userId, productId), cancellationToken);
        
        if (!result.IsSuccess)
        {
            return Json(new { success = false, error = result.Error ?? "خطا در افزودن/حذف از علاقه‌مندی‌ها" });
        }

        var isAdded = result.Value;
        var message = isAdded 
            ? "محصول به علاقه‌مندی‌ها اضافه شد." 
            : "محصول از علاقه‌مندی‌ها حذف شد.";

        return Json(new { success = true, isAdded, message });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(Guid productId, CancellationToken cancellationToken)
    {
        if (productId == Guid.Empty)
        {
            return Json(new { success = false, error = "محصول انتخاب شده معتبر نیست." });
        }

        var userId = GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Json(new { success = false, error = "لطفا ابتدا وارد حساب کاربری خود شوید." });
        }

        var result = await _mediator.Send(new RemoveWishlistItemCommand(userId, productId), cancellationToken);
        
        if (!result.IsSuccess)
        {
            return Json(new { success = false, error = result.Error ?? "خطا در حذف از علاقه‌مندی‌ها" });
        }

        return Json(new { success = true, message = "محصول از علاقه‌مندی‌ها حذف شد." });
    }

    private string? GetUserId()
        => User?.Identity?.IsAuthenticated == true
            ? User.FindFirstValue(ClaimTypes.NameIdentifier)
            : null;

    private ProductSummaryViewModel MapToSummary(Domain.Entities.Catalog.Product product)
    {
        // Calculate rating from comments
        var approvedComments = product.Comments?.Where(c => !c.IsDeleted && c.IsApproved).ToList() ?? new List<Domain.Entities.Catalog.ProductComment>();
        var rating = approvedComments.Any() 
            ? approvedComments.Average(c => c.Rating) 
            : 0.0;

        return new ProductSummaryViewModel
        {
            Id = product.Id,
            Slug = product.SeoSlug ?? string.Empty,
            Name = product.Name,
            ShortDescription = product.Summary,
            Price = product.Price,
            OriginalPrice = product.CompareAtPrice,
            IsCustomOrder = product.IsCustomOrder,
            ThumbnailUrl = product.FeaturedImagePath ?? "https://placehold.co/800x600?text=Product",
            Category = product.Category?.Name,
            Rating = rating,
            ReviewCount = approvedComments.Count,
            Tags = product.Tags.ToList()
        };
    }
}


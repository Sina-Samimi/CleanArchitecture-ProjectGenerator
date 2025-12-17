using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using TestAttarClone.Application.Commands.Catalog;
using TestAttarClone.Application.Queries.Catalog;
using TestAttarClone.SharedKernel.Authorization;
using TestAttarClone.WebSite.Areas.Seller.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TestAttarClone.WebSite.Areas.Seller.Controllers;

[Area("Seller")]
[Authorize(Policy = AuthorizationPolicies.SellerPanelAccess)]
public sealed class ProductOffersController : Controller
{
    private readonly IMediator _mediator;

    public ProductOffersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    private const string ProductOffersTabKey = "product-offers";

    private void ConfigureLayoutContext(string tabKey)
    {
        ViewData["SellerSidebar:ActiveTab"] = tabKey;
        ViewData["SellerSidebar:ActiveGroup"] = "store";
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        int page = 1,
        CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            TempData["Error"] = "کاربر احراز هویت نشده است.";
            return RedirectToAction("Index", "Dashboard");
        }

        ConfigureLayoutContext(ProductOffersTabKey);
        ViewData["Title"] = "پیشنهادات محصولات";
        ViewData["Subtitle"] = "مدیریت پیشنهادات شما برای محصولات موجود";

        var pageNumber = page < 1 ? 1 : page;
        var pageSize = 20;

        var query = new GetProductOffersQuery(
            ProductId: null,
            SellerId: userId,
            IncludeInactive: true,
            PageNumber: pageNumber,
            PageSize: pageSize);

        var result = await _mediator.Send(query, cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            TempData["Error"] = result.Error ?? "دریافت لیست پیشنهادات با خطا مواجه شد.";
            return View(new SellerProductOfferListViewModel
            {
                Offers = Array.Empty<SellerProductOfferViewModel>(),
                TotalCount = 0,
                PageNumber = 1,
                PageSize = pageSize
            });
        }

        var data = result.Value;
        var viewModel = new SellerProductOfferListViewModel
        {
            Offers = data.Offers.Select(o => new SellerProductOfferViewModel
            {
                Id = o.Id,
                ProductId = o.ProductId,
                ProductName = o.ProductName,
                ProductSlug = o.ProductSlug,
                Price = o.Price,
                CompareAtPrice = o.CompareAtPrice,
                TrackInventory = o.TrackInventory,
                StockQuantity = o.StockQuantity,
                IsActive = o.IsActive,
                IsPublished = o.IsPublished,
                PublishedAt = o.PublishedAt,
                CreatedAt = o.CreatedAt,
                UpdatedAt = o.UpdatedAt
            }).ToArray(),
            TotalCount = data.TotalCount,
            PageNumber = data.PageNumber,
            PageSize = data.PageSize
        };

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            TempData["Error"] = "کاربر احراز هویت نشده است.";
            return RedirectToAction("Index", "Dashboard");
        }

        if (id == Guid.Empty)
        {
            return NotFound();
        }

        ConfigureLayoutContext(ProductOffersTabKey);
        ViewData["Title"] = "جزئیات پیشنهاد";

        var query = new GetProductOfferDetailQuery(id);
        var result = await _mediator.Send(query, cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            TempData["Error"] = result.Error ?? "دریافت جزئیات پیشنهاد با خطا مواجه شد.";
            return RedirectToAction(nameof(Index));
        }

        var data = result.Value;

        // Verify ownership
        if (data.SellerId != userId)
        {
            TempData["Error"] = "شما اجازه دسترسی به این پیشنهاد را ندارید.";
            return RedirectToAction(nameof(Index));
        }

        var viewModel = new SellerProductOfferDetailViewModel
        {
            Id = data.Id,
            ProductId = data.ProductId,
            ProductName = data.ProductName,
            ProductSlug = data.ProductSlug,
            Price = data.Price,
            CompareAtPrice = data.CompareAtPrice,
            TrackInventory = data.TrackInventory,
            StockQuantity = data.StockQuantity,
            IsActive = data.IsActive,
            IsPublished = data.IsPublished,
            PublishedAt = data.PublishedAt,
            CreatedAt = data.CreatedAt,
            UpdatedAt = data.UpdatedAt
        };

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            TempData["Error"] = "کاربر احراز هویت نشده است.";
            return RedirectToAction("Index", "Dashboard");
        }

        if (id == Guid.Empty)
        {
            return NotFound();
        }

        ConfigureLayoutContext(ProductOffersTabKey);
        ViewData["Title"] = "ویرایش پیشنهاد";

        var query = new GetProductOfferDetailQuery(id);
        var result = await _mediator.Send(query, cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            TempData["Error"] = result.Error ?? "دریافت پیشنهاد با خطا مواجه شد.";
            return RedirectToAction(nameof(Index));
        }

        var data = result.Value;

        // Verify ownership
        if (data.SellerId != userId)
        {
            TempData["Error"] = "شما اجازه ویرایش این پیشنهاد را ندارید.";
            return RedirectToAction(nameof(Index));
        }

        var viewModel = new SellerProductOfferFormViewModel
        {
            Id = data.Id,
            ProductId = data.ProductId,
            ProductName = data.ProductName,
            Price = data.Price,
            CompareAtPrice = data.CompareAtPrice,
            TrackInventory = data.TrackInventory,
            StockQuantity = data.StockQuantity,
            IsActive = data.IsActive
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(SellerProductOfferFormViewModel model, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            TempData["Error"] = "کاربر احراز هویت نشده است.";
            return RedirectToAction("Index", "Dashboard");
        }

        if (!ModelState.IsValid)
        {
            ConfigureLayoutContext(ProductOffersTabKey);
            return View(model);
        }

        // Verify ownership
        var detailQuery = new GetProductOfferDetailQuery(model.Id);
        var detailResult = await _mediator.Send(detailQuery, cancellationToken);

        if (!detailResult.IsSuccess || detailResult.Value is null || detailResult.Value.SellerId != userId)
        {
            TempData["Error"] = "شما اجازه ویرایش این پیشنهاد را ندارید.";
            return RedirectToAction(nameof(Index));
        }

        // Seller can only update price, inventory, and active status - not publish status
        var command = new UpdateProductOfferCommand(
            model.Id,
            model.Price,
            model.CompareAtPrice,
            model.TrackInventory,
            model.StockQuantity,
            model.IsActive,
            detailResult.Value.IsPublished, // Keep current publish status - only admin can change this
            detailResult.Value.PublishedAt);

        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "ویرایش پیشنهاد با خطا مواجه شد.");
            ConfigureLayoutContext(ProductOffersTabKey);
            return View(model);
        }

        TempData["Success"] = "پیشنهاد با موفقیت ویرایش شد.";
        return RedirectToAction(nameof(Details), new { id = model.Id });
    }
}


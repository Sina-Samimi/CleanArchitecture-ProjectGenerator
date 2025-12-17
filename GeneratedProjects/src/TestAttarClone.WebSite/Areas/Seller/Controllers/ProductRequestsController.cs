using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using TestAttarClone.Application.Queries.Catalog;
using TestAttarClone.Domain.Enums;
using TestAttarClone.SharedKernel.Authorization;
using TestAttarClone.WebSite.Areas.Seller.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TestAttarClone.WebSite.Areas.Seller.Controllers;

[Area("Seller")]
[Authorize(Policy = AuthorizationPolicies.SellerPanelAccess)]
public sealed class ProductRequestsController : Controller
{
    private readonly IMediator _mediator;

    public ProductRequestsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    private const string ProductRequestsTabKey = "product-requests";

    private void ConfigureLayoutContext(string tabKey)
    {
        ViewData["Sidebar:ActiveTab"] = tabKey;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        int page = 1,
        ProductRequestStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        ConfigureLayoutContext(ProductRequestsTabKey);
        ViewData["Title"] = "درخواست‌های محصول";
        ViewData["Subtitle"] = "مدیریت درخواست‌های محصول من";

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        var pageNumber = page < 1 ? 1 : page;
        var pageSize = 20;

        var query = new GetProductRequestsQuery(pageNumber, pageSize, status, userId);
        var result = await _mediator.Send(query, cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            TempData["Error"] = result.Error ?? "دریافت لیست درخواست‌ها با خطا مواجه شد.";
            return View(new SellerProductRequestListViewModel
            {
                Requests = Array.Empty<SellerProductRequestViewModel>(),
                TotalCount = 0,
                PageNumber = 1,
                PageSize = pageSize
            });
        }

        var data = result.Value;
        var viewModel = new SellerProductRequestListViewModel
        {
            Requests = data.Requests.Select(r => new SellerProductRequestViewModel
            {
                Id = r.Id,
                Name = r.Name,
                Summary = r.Summary,
                Type = r.Type,
                Price = r.Price,
                CategoryName = r.CategoryName,
                FeaturedImagePath = r.FeaturedImagePath,
                Status = r.Status,
                CreatedAt = r.CreatedAt,
                ReviewedAt = r.ReviewedAt,
                RejectionReason = r.RejectionReason,
                ApprovedProductId = r.ApprovedProductId,
                IsCustomOrder = r.IsCustomOrder
            }).ToArray(),
            TotalCount = data.TotalCount,
            PageNumber = data.PageNumber,
            PageSize = data.PageSize,
            SelectedStatus = status
        };

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        ConfigureLayoutContext(ProductRequestsTabKey);
        ViewData["Title"] = "جزئیات درخواست محصول";

        if (id == Guid.Empty)
        {
            TempData["Error"] = "شناسه درخواست معتبر نیست.";
            return RedirectToAction(nameof(Index));
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        var query = new GetProductRequestDetailQuery(id);
        var result = await _mediator.Send(query, cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            TempData["Error"] = result.Error ?? "دریافت جزئیات درخواست با خطا مواجه شد.";
            return RedirectToAction(nameof(Index));
        }

        // Verify that this request belongs to the current seller
        if (result.Value.SellerId != userId)
        {
            TempData["Error"] = "شما دسترسی به این درخواست را ندارید.";
            return RedirectToAction(nameof(Index));
        }

        var data = result.Value;
        var viewModel = new SellerProductRequestDetailViewModel
        {
            Id = data.Id,
            Name = data.Name,
            Summary = data.Summary,
            Description = data.Description,
            Type = data.Type,
            Price = data.Price,
            TrackInventory = data.TrackInventory,
            StockQuantity = data.StockQuantity,
            CategoryName = data.CategoryName,
            FeaturedImagePath = data.FeaturedImagePath,
            DigitalDownloadPath = data.DigitalDownloadPath,
            TagList = data.TagList,
            Status = data.Status,
            CreatedAt = data.CreatedAt,
            UpdatedAt = data.UpdatedAt,
            ReviewedAt = data.ReviewedAt,
            RejectionReason = data.RejectionReason,
            ApprovedProductId = data.ApprovedProductId,
            SeoSlug = data.SeoSlug,
            IsCustomOrder = data.IsCustomOrder,
            Gallery = data.Gallery.Select(g => new SellerProductRequestGalleryImageViewModel
            {
                Id = g.Id,
                Path = g.Path,
                Order = g.Order
            }).ToArray()
        };

        ViewData["Title"] = $"جزئیات درخواست - {data.Name}";

        return View(viewModel);
    }
}


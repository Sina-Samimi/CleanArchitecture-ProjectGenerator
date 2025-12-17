using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Attar.Application.Commands.Catalog;
using Attar.Application.Interfaces;
using Attar.Application.Queries.Catalog;
using Attar.Domain.Enums;
using Attar.WebSite.Areas.Admin.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Attar.WebSite.Areas.Admin.Controllers;

[Area("Admin")]
public sealed class ProductRequestsController : Controller
{
    private readonly IMediator _mediator;
    private readonly ISellerProfileRepository _sellerProfileRepository;

    public ProductRequestsController(
        IMediator mediator,
        ISellerProfileRepository sellerProfileRepository)
    {
        _mediator = mediator;
        _sellerProfileRepository = sellerProfileRepository;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        int page = 1,
        ProductRequestStatus? status = null,
        string? sellerId = null,
        string? productName = null,
        CancellationToken cancellationToken = default)
    {
        ViewData["Title"] = "درخواست‌های محصول";
        ViewData["Subtitle"] = "مدیریت و تایید درخواست‌های محصول فروشندگان";

        var pageNumber = page < 1 ? 1 : page;
        var pageSize = 20;

        var query = new GetProductRequestsQuery(pageNumber, pageSize, status, sellerId, productName);
        var result = await _mediator.Send(query, cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            TempData["Error"] = result.Error ?? "دریافت لیست درخواست‌ها با خطا مواجه شد.";
            return View(new ProductRequestListViewModel
            {
                Requests = Array.Empty<ProductRequestViewModel>(),
                TotalCount = 0,
                PageNumber = 1,
                PageSize = pageSize
            });
        }

        var data = result.Value;
        var viewModel = new ProductRequestListViewModel
        {
            Requests = data.Requests.Select(r => new ProductRequestViewModel
            {
                Id = r.Id,
                Name = r.Name,
                Summary = r.Summary,
                Type = r.Type,
                Price = r.Price,
                CategoryName = r.CategoryName,
                FeaturedImagePath = r.FeaturedImagePath,
                SellerId = r.SellerId,
                SellerName = r.SellerName,
                SellerPhone = r.SellerPhone,
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
            SelectedStatus = status,
            SelectedSellerId = sellerId
        };

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "جزئیات درخواست محصول";

        if (id == Guid.Empty)
        {
            TempData["Error"] = "شناسه درخواست معتبر نیست.";
            return RedirectToAction(nameof(Index));
        }

        var query = new GetProductRequestDetailQuery(id);
        var result = await _mediator.Send(query, cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            TempData["Error"] = result.Error ?? "دریافت جزئیات درخواست با خطا مواجه شد.";
            return RedirectToAction(nameof(Index));
        }

        var data = result.Value;
        var viewModel = new ProductRequestDetailViewModel
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
            SellerId = data.SellerId,
            SellerName = data.SellerName,
            SellerPhone = data.SellerPhone,
            Status = data.Status,
            CreatedAt = data.CreatedAt,
            UpdatedAt = data.UpdatedAt,
            ReviewedAt = data.ReviewedAt,
            ReviewerId = data.ReviewerId,
            ReviewerName = data.ReviewerName,
            RejectionReason = data.RejectionReason,
            ApprovedProductId = data.ApprovedProductId,
            SeoSlug = data.SeoSlug,
            IsCustomOrder = data.IsCustomOrder,
            Gallery = data.Gallery.Select(g => new ProductRequestGalleryImageViewModel
            {
                Id = g.Id,
                Path = g.Path,
                Order = g.Order
            }).ToArray()
        };

        ViewData["Title"] = $"جزئیات درخواست - {data.Name}";

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(
        Guid id,
        bool isPublished = true,
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            TempData["Error"] = "شناسه درخواست معتبر نیست.";
            return RedirectToAction(nameof(Index));
        }

        var command = new ApproveProductRequestCommand(id, isPublished);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error ?? "تایید درخواست با خطا مواجه شد.";
        }
        else
        {
            TempData["Success"] = "درخواست محصول با موفقیت تایید و به جدول محصولات اضافه شد.";
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(
        Guid id,
        string? rejectionReason,
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            TempData["Error"] = "شناسه درخواست معتبر نیست.";
            return RedirectToAction(nameof(Index));
        }

        var command = new RejectProductRequestCommand(id, rejectionReason);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error ?? "رد درخواست با خطا مواجه شد.";
        }
        else
        {
            TempData["Success"] = "درخواست محصول رد شد.";
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> SearchSellers(string? term, CancellationToken cancellationToken)
    {
        var sellers = await _sellerProfileRepository.GetActiveAsync(cancellationToken);
        var search = term?.Trim();

        var filtered = string.IsNullOrWhiteSpace(search)
            ? sellers
            : sellers
                .Where(s =>
                    (!string.IsNullOrWhiteSpace(s.DisplayName) &&
                     s.DisplayName.Contains(search, StringComparison.OrdinalIgnoreCase))
                    || (!string.IsNullOrWhiteSpace(s.ContactPhone) &&
                        s.ContactPhone.Contains(search, StringComparison.OrdinalIgnoreCase)))
                .ToArray();

        var result = filtered
            .OrderBy(s => s.DisplayName)
            .Take(20)
            .Select(s => new
            {
                id = s.UserId,
                name = s.DisplayName,
                phone = s.ContactPhone
            });

        return Json(result);
    }
}


using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Attar.Application.Commands.Orders;
using Attar.Application.Interfaces;
using Attar.Application.Queries.Catalog;
using Attar.Application.Queries.Orders;
using Attar.Domain.Enums;
using Attar.SharedKernel.Authorization;
using Attar.SharedKernel.BaseTypes;
using Attar.SharedKernel.Extensions;
using Attar.WebSite.Areas.Seller.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Attar.WebSite.Areas.Seller.Controllers;

[Area("Seller")]
[Authorize(Policy = AuthorizationPolicies.SellerPanelAccess)]
public sealed class OrdersController : Controller
{
    private readonly IMediator _mediator;
    private readonly IProductRepository _productRepository;

    public OrdersController(IMediator mediator, IProductRepository productRepository)
    {
        _mediator = mediator;
        _productRepository = productRepository;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        [FromQuery] string? searchTerm,
        [FromQuery] InvoiceStatus? status,
        [FromQuery] string? fromDate,
        [FromQuery] string? toDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        ViewData["Title"] = "سفارشات";
        ViewData["Subtitle"] = "لیست سفارشات محصولات شما";
        ViewData["Sidebar:ActiveTab"] = "orders";

        var cancellationToken = HttpContext.RequestAborted;
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            TempData["Error"] = "کاربر احراز هویت نشده است.";
            return RedirectToAction("Index", "Products");
        }

        DateTimeOffset? fromDateParsed = UserFilterFormatting.ParsePersianDate(fromDate, toExclusiveEnd: false, out _);
        DateTimeOffset? toDateParsed = UserFilterFormatting.ParsePersianDate(toDate, toExclusiveEnd: true, out _);

        // نادیده گرفتن فیلتر Pending برای فروشندگان
        InvoiceStatus? filteredStatus = status == InvoiceStatus.Pending ? null : status;

        var query = new GetOrdersQuery(
            SellerId: userId,
            UserId: null,
            Status: filteredStatus,
            FromDate: fromDateParsed,
            ToDate: toDateParsed,
            PageNumber: page,
            PageSize: pageSize);

        var result = await _mediator.Send(query, cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            TempData["Error"] = result.Error ?? "خطا در دریافت لیست سفارشات.";
            return View(new SellerOrderIndexViewModel
            {
                Orders = Array.Empty<SellerOrderListItemViewModel>(),
                TotalCount = 0,
                PageNumber = page,
                PageSize = pageSize
            });
        }

        var data = result.Value;
        
        // Load variant information for orders that have variants
        var variantIds = data.Items
            .Where(o => o.VariantId.HasValue)
            .Select(o => o.VariantId!.Value)
            .Distinct()
            .ToList();
        
        var variantInfoMap = new Dictionary<Guid, string>();
        if (variantIds.Any())
        {
            var products = await _productRepository.GetByIdsAsync(
                data.Items.Where(o => o.ProductId.HasValue).Select(o => o.ProductId!.Value).Distinct().ToList(),
                cancellationToken);
            
            foreach (var product in products)
            {
                var attributeMap = product.VariantAttributes.ToDictionary(attr => attr.Id);
                
                foreach (var variantId in variantIds)
                {
                    var variant = product.Variants.FirstOrDefault(v => v.Id == variantId);
                    if (variant is not null)
                    {
                        var variantOptions = variant.Options
                            .Where(opt => attributeMap.ContainsKey(opt.VariantAttributeId))
                            .OrderBy(opt => attributeMap[opt.VariantAttributeId].DisplayOrder)
                            .ThenBy(opt => attributeMap[opt.VariantAttributeId].Name)
                            .Select(opt => $"{attributeMap[opt.VariantAttributeId].Name}: {opt.Value}")
                            .ToList();
                        
                        variantInfoMap[variantId] = string.Join(", ", variantOptions);
                    }
                }
            }
        }
        
        var orders = data.Items.Select(order => new SellerOrderListItemViewModel
        {
            InvoiceId = order.InvoiceId,
            InvoiceNumber = order.InvoiceNumber,
            OrderDate = order.OrderDate,
            Status = order.Status,
            UserId = order.UserId,
            InvoiceItemId = order.InvoiceItemId,
            ProductName = order.ProductName,
            Quantity = order.Quantity,
            UnitPrice = order.UnitPrice,
            Total = order.Total,
            ProductId = order.ProductId,
            VariantId = order.VariantId,
            VariantInfo = order.VariantId.HasValue && variantInfoMap.TryGetValue(order.VariantId.Value, out var info) 
                ? info 
                : null
        }).ToArray();

        var model = new SellerOrderIndexViewModel
        {
            Orders = orders,
            TotalCount = data.TotalCount,
            PageNumber = data.PageNumber,
            PageSize = data.PageSize,
            TotalPages = data.TotalPages,
            Filter = new SellerOrderFilterViewModel
            {
                SearchTerm = searchTerm,
                Status = status,
                FromDate = fromDate,
                ToDate = toDate
            }
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid invoiceId)
    {
        if (invoiceId == Guid.Empty)
        {
            TempData["Error"] = "شناسه فاکتور معتبر نیست.";
            return RedirectToAction(nameof(Index));
        }

        // Redirect to invoice details if available, or show order details
        return RedirectToAction("Details", "Invoices", new { area = "Admin", id = invoiceId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateShipmentStatus(
        Guid invoiceItemId,
        Guid? productId,
        string statusText,
        string? statusDatePersian,
        string? trackingNumber,
        string? notes)
    {
        if (invoiceItemId == Guid.Empty)
        {
            TempData["Error"] = "شناسه آیتم فاکتور معتبر نیست.";
            return RedirectToAction(nameof(Index));
        }

        if (string.IsNullOrWhiteSpace(statusText))
        {
            TempData["Error"] = "وضعیت سفارش الزامی است.";
            return RedirectToAction(nameof(Index));
        }

        var cancellationToken = HttpContext.RequestAborted;
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            TempData["Error"] = "کاربر احراز هویت نشده است.";
            return RedirectToAction(nameof(Index));
        }

        // Verify product ownership if productId is provided
        if (productId.HasValue)
        {
            var productResult = await _mediator.Send(
                new GetSellerProductDetailQuery(productId.Value, userId),
                cancellationToken);
            if (!productResult.IsSuccess || productResult.Value is null)
            {
                TempData["Error"] = "محصول مورد نظر یافت نشد یا شما دسترسی ندارید.";
                return RedirectToAction(nameof(Index));
            }
        }
        
        // Parse Persian date
        DateTimeOffset statusDate = DateTimeOffset.UtcNow;
        if (!string.IsNullOrWhiteSpace(statusDatePersian))
        {
            var parsedDate = UserFilterFormatting.ParsePersianDate(statusDatePersian, toExclusiveEnd: false, out _);
            if (parsedDate.HasValue)
            {
                statusDate = parsedDate.Value;
            }
        }
        
        // Combine status text with notes
        var combinedNotes = statusText.Trim();
        if (!string.IsNullOrWhiteSpace(notes))
        {
            combinedNotes = $"{combinedNotes}\n\n{notes.Trim()}";
        }
        
        // Check if tracking already exists
        var existingTrackingResult = await _mediator.Send(
            new GetShipmentTrackingQuery(invoiceItemId), 
            cancellationToken);

        Result<Guid> result;
        
        // Use Preparing as default status, store custom text in notes
        if (existingTrackingResult.IsSuccess && existingTrackingResult.Value is not null)
        {
            // Update existing tracking
            var updateCommand = new UpdateShipmentTrackingCommand(
                existingTrackingResult.Value.Id,
                ShipmentStatus.Preparing, // Default status
                statusDate,
                trackingNumber,
                combinedNotes); // Store status text in notes
            result = await _mediator.Send(updateCommand, cancellationToken);
        }
        else
        {
            // Create new tracking
            var createCommand = new CreateShipmentTrackingCommand(
                invoiceItemId,
                ShipmentStatus.Preparing, // Default status
                statusDate,
                trackingNumber,
                combinedNotes); // Store status text in notes
            result = await _mediator.Send(createCommand, cancellationToken);
        }

        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error ?? "خطا در ثبت وضعیت سفارش.";
        }
        else
        {
            TempData["Success"] = "وضعیت سفارش با موفقیت ثبت شد.";
        }

        return RedirectToAction(nameof(Index));
    }
}


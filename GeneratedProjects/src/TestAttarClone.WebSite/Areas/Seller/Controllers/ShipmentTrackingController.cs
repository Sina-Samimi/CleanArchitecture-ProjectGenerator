using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TestAttarClone.Application.Commands.Orders;
using TestAttarClone.Application.Queries.Catalog;
using TestAttarClone.Application.Queries.Identity.GetUsersByIds;
using TestAttarClone.Application.Interfaces;
using TestAttarClone.WebSite.Areas.Seller.Models;
using TestAttarClone.SharedKernel.Authorization;
using TestAttarClone.SharedKernel.BaseTypes;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TestAttarClone.WebSite.Areas.Seller.Controllers;

[Area("Seller")]
[Authorize(Policy = AuthorizationPolicies.SellerPanelAccess)]
public sealed class ShipmentTrackingController : Controller
{
    private readonly IMediator _mediator;
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IShipmentTrackingRepository _trackingRepository;
    private readonly IProductRepository _productRepository;

    public ShipmentTrackingController(
        IMediator mediator,
        IInvoiceRepository invoiceRepository,
        IShipmentTrackingRepository trackingRepository,
        IProductRepository productRepository)
    {
        _mediator = mediator;
        _invoiceRepository = invoiceRepository;
        _trackingRepository = trackingRepository;
        _productRepository = productRepository;
    }

    [HttpGet]
    public async Task<IActionResult> Index(Guid productId)
    {
        if (productId == Guid.Empty)
        {
            TempData["Error"] = "شناسه محصول معتبر نیست.";
            return RedirectToAction("Index", "Products");
        }

        var cancellationToken = HttpContext.RequestAborted;
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(userId))
        {
            TempData["Error"] = "کاربر احراز هویت نشده است.";
            return RedirectToAction("Index", "Products");
        }

        // Verify product ownership
        var productResult = await _mediator.Send(new GetSellerProductDetailQuery(productId, userId), cancellationToken);
        if (!productResult.IsSuccess || productResult.Value is null)
        {
            TempData["Error"] = "محصول مورد نظر یافت نشد یا شما دسترسی ندارید.";
            return RedirectToAction("Index", "Products");
        }

        var product = productResult.Value;

        // Get invoice items for this product
        var invoiceItems = await _invoiceRepository.GetProductInvoiceItemsAsync(productId, cancellationToken);

        // Get all trackings for these invoice items
        var invoiceItemIds = invoiceItems.Select(item => item.Id).ToArray();
        var trackings = new List<Domain.Entities.Orders.ShipmentTracking>();
        
        foreach (var itemId in invoiceItemIds)
        {
            var tracking = await _trackingRepository.GetByInvoiceItemIdAsync(itemId, cancellationToken);
            if (tracking is not null)
            {
                trackings.Add(tracking);
            }
        }

        var trackingMap = trackings.ToDictionary(t => t.InvoiceItemId);

        // Load variant information for invoice items that have variants
        var variantIds = invoiceItems
            .Where(item => item.VariantId.HasValue)
            .Select(item => item.VariantId!.Value)
            .Distinct()
            .ToList();
        
        var variantInfoMap = new Dictionary<Guid, string>();
            if (variantIds.Any())
            {
                var productEntity = await _productRepository.GetWithDetailsAsync(productId, cancellationToken);
                if (productEntity is not null)
                {
                    var attributeMap = productEntity.VariantAttributes.ToDictionary(attr => attr.Id);
                    
                    foreach (var variantId in variantIds)
                    {
                        var variant = productEntity.Variants.FirstOrDefault(v => v.Id == variantId);
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

        // Get unique user IDs from invoices
        var userIds = invoiceItems
            .Where(item => item.ItemType == Domain.Enums.InvoiceItemType.Product && !string.IsNullOrWhiteSpace(item.Invoice.UserId))
            .Select(item => item.Invoice.UserId!)
            .Distinct()
            .ToArray();

        // Fetch user details
        var userLookups = new Dictionary<string, (string DisplayName, string? PhoneNumber)>(StringComparer.Ordinal);
        if (userIds.Length > 0)
        {
            var userLookupResult = await _mediator.Send(new GetUsersByIdsQuery(userIds), cancellationToken);
            if (userLookupResult.IsSuccess && userLookupResult.Value is not null)
            {
                foreach (var kvp in userLookupResult.Value)
                {
                    userLookups[kvp.Key] = (kvp.Value.DisplayName, kvp.Value.PhoneNumber);
                }
            }
        }

        var model = new SellerShipmentTrackingIndexViewModel
        {
            ProductId = productId,
            ProductName = product.Name,
            InvoiceItems = invoiceItems
                .Where(item => item.ItemType == Domain.Enums.InvoiceItemType.Product)
                .Select(item =>
                {
                    var tracking = trackingMap.GetValueOrDefault(item.Id);
                    var userId = item.Invoice.UserId;
                    var (displayName, phoneNumber) = !string.IsNullOrWhiteSpace(userId) && userLookups.TryGetValue(userId, out var userInfo)
                        ? userInfo
                        : (userId ?? "-", (string?)null);

                    var variantInfo = item.VariantId.HasValue && variantInfoMap.TryGetValue(item.VariantId.Value, out var info)
                        ? info
                        : null;

                    return new SellerShipmentTrackingItemViewModel
                    {
                        InvoiceItemId = item.Id,
                        InvoiceId = item.InvoiceId,
                        InvoiceNumber = item.Invoice.InvoiceNumber,
                        CustomerName = displayName,
                        CustomerPhone = phoneNumber,
                        Quantity = (int)item.Quantity, // Convert to int
                        Status = tracking?.Status,
                        TrackingNumber = tracking?.TrackingNumber,
                        StatusDate = tracking?.StatusDate,
                        Notes = tracking?.Notes,
                        TrackingId = tracking?.Id,
                        VariantId = item.VariantId,
                        VariantInfo = variantInfo
                    };
                })
                .ToArray()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateTracking(SellerShipmentTrackingFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "لطفاً تمام فیلدهای الزامی را پر کنید.";
            return RedirectToAction("Index", new { productId = model.ProductId });
        }

        var cancellationToken = HttpContext.RequestAborted;
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(userId))
        {
            TempData["Error"] = "کاربر احراز هویت نشده است.";
            return RedirectToAction("Index", "Products");
        }

        // Verify product ownership
        var productResult = await _mediator.Send(new GetSellerProductDetailQuery(model.ProductId, userId), cancellationToken);
        if (!productResult.IsSuccess || productResult.Value is null)
        {
            TempData["Error"] = "محصول مورد نظر یافت نشد یا شما دسترسی ندارید.";
            return RedirectToAction("Index", "Products");
        }

        if (string.IsNullOrWhiteSpace(model.StatusText))
        {
            TempData["Error"] = "وضعیت سفارش الزامی است.";
            return RedirectToAction("Index", new { productId = model.ProductId });
        }

        var statusDate = model.StatusDate;
        if (!string.IsNullOrWhiteSpace(model.StatusDatePersian))
        {
            // Parse Persian date using UserFilterFormatting
            var parsedDate = TestAttarClone.SharedKernel.Extensions.UserFilterFormatting.ParsePersianDate(
                model.StatusDatePersian, 
                toExclusiveEnd: false, 
                out _);
            if (parsedDate.HasValue)
            {
                statusDate = parsedDate.Value.DateTime;
            }
        }

        // Combine status text with notes
        var combinedNotes = model.StatusText.Trim();
        if (!string.IsNullOrWhiteSpace(model.Notes))
        {
            combinedNotes = $"{combinedNotes}\n\n{model.Notes.Trim()}";
        }

        Result<Guid> result;
        
        if (model.Id.HasValue)
        {
            var updateCommand = new UpdateShipmentTrackingCommand(
                model.Id.Value,
                TestAttarClone.Domain.Enums.ShipmentStatus.Preparing, // Default status
                statusDate,
                model.TrackingNumber,
                combinedNotes); // Store status text in notes
            result = await _mediator.Send(updateCommand, cancellationToken);
        }
        else
        {
            var createCommand = new CreateShipmentTrackingCommand(
                model.InvoiceItemId,
                TestAttarClone.Domain.Enums.ShipmentStatus.Preparing, // Default status
                statusDate,
                model.TrackingNumber,
                combinedNotes); // Store status text in notes
            result = await _mediator.Send(createCommand, cancellationToken);
        }

        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error ?? "خطا در ذخیره اطلاعات پیگیری ارسال.";
        }
        else
        {
            TempData["Success"] = "اطلاعات پیگیری ارسال با موفقیت ذخیره شد.";
        }

        return RedirectToAction("Index", new { productId = model.ProductId });
    }
}


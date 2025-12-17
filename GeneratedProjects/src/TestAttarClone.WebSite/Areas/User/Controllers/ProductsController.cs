using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TestAttarClone.Application.DTOs.Catalog;
using TestAttarClone.Application.DTOs.Orders;
using TestAttarClone.Application.Queries.Catalog;
using TestAttarClone.Application.Queries.Orders;
using TestAttarClone.Domain.Entities;
using TestAttarClone.Domain.Enums;
using TestAttarClone.SharedKernel.Extensions;
using TestAttarClone.WebSite.Areas.User.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace TestAttarClone.WebSite.Areas.User.Controllers;

[Area("User")]
[Authorize]
public sealed class ProductsController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IMediator _mediator;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(
        UserManager<ApplicationUser> userManager,
        IMediator mediator,
        ILogger<ProductsController> logger)
    {
        _userManager = userManager;
        _mediator = mediator;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] UserProductLibraryFilterRequest? filterRequest, CancellationToken cancellationToken)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        var queryResult = await _mediator.Send(new GetUserPurchasedProductsQuery(user.Id), cancellationToken);

        IReadOnlyCollection<UserPurchasedProductDto> purchases;
        if (!queryResult.IsSuccess)
        {
            _logger.LogWarning(
                "Failed to fetch purchased products for user {UserId}: {Error}",
                user.Id,
                queryResult.Error);
            TempData["Error"] = queryResult.Error ?? "امکان دریافت لیست محصولات خریداری شده وجود ندارد.";
            purchases = Array.Empty<UserPurchasedProductDto>();
        }
        else
        {
            purchases = queryResult.Value ?? Array.Empty<UserPurchasedProductDto>();
        }

        var filter = filterRequest ?? new UserProductLibraryFilterRequest();
        
        // Get shipment trackings for user
        var trackingsResult = await _mediator.Send(new GetUserShipmentTrackingsQuery(user.Id), cancellationToken);
        var trackings = trackingsResult.IsSuccess && trackingsResult.Value is not null
            ? trackingsResult.Value.ToDictionary(t => t.InvoiceItemId)
            : new Dictionary<Guid, ShipmentTrackingDto>();
        
        var viewModel = MapToViewModel(purchases, filter, trackings);

        PrepareLayoutMetadata(user);
        ViewData["Title"] = "محصولات خریداری شده";
        ViewData["Subtitle"] = "کتابخانه محصولات خریداری شده و دسترسی فایل‌های دانلودی";
        ViewData["Sidebar:ActiveTab"] = "library";
        ViewData["TitleSuffix"] = "پنل کاربری";
        ViewData["ShowSearch"] = false;

        return View(viewModel);
    }

    private UserProductLibraryViewModel MapToViewModel(
        IReadOnlyCollection<UserPurchasedProductDto> items,
        UserProductLibraryFilterRequest filter,
        Dictionary<Guid, ShipmentTrackingDto> trackings)
    {
        var filteredItems = ApplyFilters(items, filter);
        var totalFilteredCount = filteredItems.Count;
        
        var pageNumber = Math.Max(1, filter.Page);
        var pageSize = Math.Max(1, Math.Min(100, filter.PageSize));
        
        var orderedItems = filteredItems
            .Select(dto => MapPurchase(dto, trackings))
            .OrderByDescending(purchase => purchase.PurchasedAt)
            .ThenBy(purchase => purchase.Name)
            .ToList();
        
        var pagedItems = orderedItems
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToArray();

        var invoiceGroups = filteredItems
            .GroupBy(item => item.InvoiceId)
            .ToArray();

        var totalPaid = invoiceGroups.Sum(group => group.First().InvoicePaidAmount);
        var outstandingTotal = invoiceGroups.Sum(group => group.First().InvoiceOutstandingAmount);
        var downloadableCount = orderedItems.Count(purchase => purchase.CanDownload);
        var digitalCount = orderedItems.Count(purchase => purchase.IsDigital);

        var metrics = new[]
        {
            new UserProductLibraryMetricViewModel
            {
                Icon = "bi-bag-check",
                Label = "کل محصولات خریداری شده",
                Value = totalFilteredCount.ToString("N0"),
                Description = totalFilteredCount == 0
                    ? "هنوز محصولی خریداری نکرده‌اید"
                    : "شامل دوره‌ها و فایل‌های دانلودی",
                Tone = "primary"
            },
            new UserProductLibraryMetricViewModel
            {
                Icon = "bi-cloud-arrow-down",
                Label = "دانلودهای قابل دسترس",
                Value = downloadableCount.ToString("N0"),
                Description = digitalCount == 0
                    ? "محصول دانلودی در کتابخانه شما وجود ندارد"
                    : $"از {digitalCount:N0} فایل دانلودی",
                Tone = "info"
            },
            new UserProductLibraryMetricViewModel
            {
                Icon = "bi-cash-coin",
                Label = "مجموع پرداختی",
                Value = totalPaid.ToString("N0") + " ریال",
                Description = outstandingTotal > 0
                    ? $"{outstandingTotal:N0} ریال تسویه نشده"
                    : "بدون بدهی فعال",
                Tone = outstandingTotal > 0 ? "warning" : "success"
            }
        };

        return new UserProductLibraryViewModel
        {
            Metrics = metrics,
            Purchases = pagedItems,
            Filter = new UserProductLibraryFilterViewModel
            {
                Search = filter.Search,
                Type = filter.Type,
                Status = filter.Status,
                Page = pageNumber,
                PageSize = pageSize
            },
            TotalPurchases = items.Count,
            FilteredPurchases = totalFilteredCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    private static IReadOnlyCollection<UserPurchasedProductDto> ApplyFilters(
        IReadOnlyCollection<UserPurchasedProductDto> source,
        UserProductLibraryFilterRequest filter)
    {
        IEnumerable<UserPurchasedProductDto> query = source;

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim();
            query = query.Where(item =>
                item.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                (!string.IsNullOrWhiteSpace(item.Summary) && item.Summary.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                item.InvoiceNumber.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        if (filter.Type is not null)
        {
            query = filter.Type switch
            {
                UserProductTypeFilter.Digital => query.Where(item => item.ProductType == ProductType.Digital),
                UserProductTypeFilter.Physical => query.Where(item => item.ProductType is not null && item.ProductType != ProductType.Digital),
                _ => query
            };
        }

        if (filter.Status is not null)
        {
            query = filter.Status switch
            {
                UserProductStatusFilter.Paid => query.Where(item => item.InvoiceStatus == InvoiceStatus.Paid),
                UserProductStatusFilter.PartiallyPaid => query.Where(item => item.InvoiceStatus == InvoiceStatus.PartiallyPaid),
                UserProductStatusFilter.Pending => query.Where(item => item.InvoiceStatus == InvoiceStatus.Pending),
                UserProductStatusFilter.Overdue => query.Where(item => item.InvoiceStatus == InvoiceStatus.Overdue),
                _ => query
            };
        }

        return query.ToArray();
    }

    private static UserPurchasedProductViewModel MapPurchase(UserPurchasedProductDto dto, Dictionary<Guid, ShipmentTrackingDto> trackings)
    {
        var isDigital = dto.ProductType == ProductType.Digital;
        var hasClearedBalance = Math.Abs(dto.InvoiceOutstandingAmount) <= 0.01m;
        var canDownload = isDigital
            && !string.IsNullOrWhiteSpace(dto.DigitalDownloadPath)
            && dto.InvoiceStatus == InvoiceStatus.Paid
            && hasClearedBalance;

        var statusBadge = dto.InvoiceStatus switch
        {
            InvoiceStatus.Paid => "badge bg-success-subtle text-success-emphasis",
            InvoiceStatus.PartiallyPaid => "badge bg-warning-subtle text-warning-emphasis",
            InvoiceStatus.Pending => "badge bg-info-subtle text-info-emphasis",
            InvoiceStatus.Overdue => "badge bg-danger-subtle text-danger-emphasis",
            _ => "badge bg-secondary-subtle text-secondary-emphasis"
        };

        var typeText = dto.ProductType switch
        {
            ProductType.Digital => "دانلودی",
            ProductType.Physical => "فیزیکی",
            _ => "محصول"
        };

        ShipmentTrackingInfoViewModel? shipmentTracking = null;
        if (trackings.TryGetValue(dto.InvoiceItemId, out var tracking))
        {
            var statusText = tracking.Status switch
            {
                ShipmentStatus.Preparing => "در حال آماده سازی",
                ShipmentStatus.DeliveredToPost => "تحویل به پست",
                ShipmentStatus.Shipped => "ارسال شده",
                ShipmentStatus.Delivered => "دریافت شده",
                _ => "نامشخص"
            };

            var statusBadgeClass = tracking.Status switch
            {
                ShipmentStatus.Preparing => "badge bg-info-subtle text-info-emphasis",
                ShipmentStatus.DeliveredToPost => "badge bg-warning-subtle text-warning-emphasis",
                ShipmentStatus.Shipped => "badge bg-primary-subtle text-primary-emphasis",
                ShipmentStatus.Delivered => "badge bg-success-subtle text-success-emphasis",
                _ => "badge bg-secondary-subtle text-secondary-emphasis"
            };

            shipmentTracking = new ShipmentTrackingInfoViewModel
            {
                Status = tracking.Status,
                StatusText = statusText,
                StatusBadgeClass = statusBadgeClass,
                TrackingNumber = tracking.TrackingNumber,
                StatusDate = tracking.StatusDate,
                Notes = tracking.Notes
            };
        }

        return new UserPurchasedProductViewModel
        {
            InvoiceId = dto.InvoiceId,
            InvoiceNumber = dto.InvoiceNumber,
            InvoiceItemId = dto.InvoiceItemId,
            ProductId = dto.ProductId,
            Name = dto.Name,
            Summary = dto.Summary,
            CategoryName = dto.CategoryName,
            Type = typeText,
            IsDigital = isDigital,
            CanDownload = canDownload,
            Status = dto.InvoiceStatus.GetDisplayName(),
            StatusBadgeClass = statusBadge,
            PurchasedAt = dto.PurchasedAt,
            Quantity = dto.Quantity,
            UnitPrice = dto.UnitPrice,
            Total = dto.Total,
            InvoiceGrandTotal = dto.InvoiceGrandTotal,
            InvoicePaidAmount = dto.InvoicePaidAmount,
            InvoiceOutstandingAmount = dto.InvoiceOutstandingAmount,
            ThumbnailPath = dto.FeaturedImagePath,
            DownloadUrl = canDownload ? dto.DigitalDownloadPath : null,
            ShipmentTracking = shipmentTracking
        };
    }

    private void PrepareLayoutMetadata(ApplicationUser user)
    {
        var displayName = string.IsNullOrWhiteSpace(user.FullName) ? "کاربر آرسیس" : user.FullName.Trim();
        var emailDisplay = string.IsNullOrWhiteSpace(user.Email) ? "ایمیل ثبت نشده" : user.Email;
        var phoneDisplay = string.IsNullOrWhiteSpace(user.PhoneNumber) ? "شماره ثبت نشده" : user.PhoneNumber;

        ViewData["AccountName"] = displayName;
        ViewData["AccountInitial"] = displayName.Length > 0 ? displayName[0].ToString() : "ک";
        ViewData["AccountEmail"] = emailDisplay;
        ViewData["AccountPhone"] = phoneDisplay;
        ViewData["Sidebar:Email"] = emailDisplay;
        ViewData["Sidebar:Phone"] = phoneDisplay;
        ViewData["GreetingSubtitle"] = "کتابخانه محصولات شما";
        ViewData["GreetingTitle"] = $"سلام، {displayName} 📚";
        ViewData["AccountAvatarUrl"] = user.AvatarPath;
        ViewData["Sidebar:Completion"] = 100;
    }
}

using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TestAttarClone.Application.Commands.Catalog;
using TestAttarClone.Application.Queries.Catalog;
using TestAttarClone.SharedKernel.Extensions;
using TestAttarClone.WebSite.Areas.Admin.Models;
using TestAttarClone.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace TestAttarClone.WebSite.Areas.Admin.Controllers;

[Area("Admin")]
public sealed class ProductOffersController : Controller
{
    private readonly IMediator _mediator;
    private readonly ISellerProfileRepository _sellerProfileRepository;

    public ProductOffersController(
        IMediator mediator,
        ISellerProfileRepository sellerProfileRepository)
    {
        _mediator = mediator;
        _sellerProfileRepository = sellerProfileRepository;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        int page = 1,
        Guid? productId = null,
        string? sellerId = null,
        string? productName = null,
        bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        ViewData["Title"] = "پیشنهادات محصولات";
        ViewData["Subtitle"] = "مدیریت پیشنهادات فروشندگان برای محصولات";

        var pageNumber = page < 1 ? 1 : page;
        var pageSize = 20;

        var query = new GetProductOffersQuery(
            ProductId: productId,
            SellerId: sellerId,
            ProductName: productName,
            IncludeInactive: includeInactive,
            PageNumber: pageNumber,
            PageSize: pageSize);

        var result = await _mediator.Send(query, cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            TempData["Error"] = result.Error ?? "دریافت لیست پیشنهادات با خطا مواجه شد.";
            return View(new ProductOfferListViewModel
            {
                Offers = Array.Empty<ProductOfferViewModel>(),
                TotalCount = 0,
                PageNumber = 1,
                PageSize = pageSize
            });
        }

        var data = result.Value;
        var viewModel = new ProductOfferListViewModel
        {
            Offers = data.Offers.Select(o => new ProductOfferViewModel
            {
                Id = o.Id,
                ProductId = o.ProductId,
                ProductName = o.ProductName,
                ProductSlug = o.ProductSlug,
                SellerId = o.SellerId,
                SellerName = o.SellerName,
                SellerPhone = o.SellerPhone,
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
            PageSize = data.PageSize,
            SelectedProductId = productId,
            SelectedSellerId = sellerId
        };

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
        {
            return NotFound();
        }

        ViewData["Title"] = "جزئیات پیشنهاد محصول";

        var query = new GetProductOfferDetailQuery(id);
        var result = await _mediator.Send(query, cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            TempData["Error"] = result.Error ?? "دریافت جزئیات پیشنهاد با خطا مواجه شد.";
            return RedirectToAction(nameof(Index));
        }

        var data = result.Value;
        var viewModel = new ProductOfferDetailViewModel
        {
            Id = data.Id,
            ProductId = data.ProductId,
            ProductName = data.ProductName,
            ProductSlug = data.ProductSlug,
            SellerId = data.SellerId,
            SellerName = data.SellerName,
            SellerPhone = data.SellerPhone,
            Price = data.Price,
            CompareAtPrice = data.CompareAtPrice,
            TrackInventory = data.TrackInventory,
            StockQuantity = data.StockQuantity,
            IsActive = data.IsActive,
            IsPublished = data.IsPublished,
            PublishedAt = data.PublishedAt,
            CreatedAt = data.CreatedAt,
            UpdatedAt = data.UpdatedAt,
            ApprovedFromRequestId = data.ApprovedFromRequestId
        };

        return View(viewModel);
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

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
        {
            return NotFound();
        }

        ViewData["Title"] = "ویرایش پیشنهاد محصول";

        var query = new GetProductOfferDetailQuery(id);
        var result = await _mediator.Send(query, cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            TempData["Error"] = result.Error ?? "دریافت پیشنهاد با خطا مواجه شد.";
            return RedirectToAction(nameof(Index));
        }

        var data = result.Value;
        
        string? publishedAtPersian = null;
        string? publishedAtTime = null;
        
        if (data.PublishedAt.HasValue)
        {
            var local = data.PublishedAt.Value.ToLocalTime();
            var calendar = new PersianCalendar();
            publishedAtPersian = string.Format(
                CultureInfo.InvariantCulture,
                "{0:0000}-{1:00}-{2:00}",
                calendar.GetYear(local.DateTime),
                calendar.GetMonth(local.DateTime),
                calendar.GetDayOfMonth(local.DateTime));
            publishedAtTime = local.ToString("HH:mm", CultureInfo.InvariantCulture);
        }
        
        var viewModel = new ProductOfferFormViewModel
        {
            Id = data.Id,
            ProductId = data.ProductId,
            ProductName = data.ProductName,
            Price = data.Price,
            CompareAtPrice = data.CompareAtPrice,
            TrackInventory = data.TrackInventory,
            StockQuantity = data.StockQuantity,
            IsActive = data.IsActive,
            IsPublished = data.IsPublished,
            PublishedAt = data.PublishedAt,
            PublishedAtPersian = publishedAtPersian,
            PublishedAtTime = publishedAtTime
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ProductOfferFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        DateTimeOffset? publishedAt = null;
        if (model.IsPublished && !string.IsNullOrWhiteSpace(model.PublishedAtPersian))
        {
            if (TryConvertPublishedAt(model, out var convertedDate, out var error))
            {
                publishedAt = convertedDate;
            }
            else if (!string.IsNullOrWhiteSpace(error))
            {
                ModelState.AddModelError(nameof(ProductOfferFormViewModel.PublishedAt), error);
                return View(model);
            }
        }

        var command = new UpdateProductOfferCommand(
            model.Id,
            model.Price,
            model.CompareAtPrice,
            model.TrackInventory,
            model.StockQuantity,
            model.IsActive,
            model.IsPublished,
            publishedAt);

        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "ویرایش پیشنهاد با خطا مواجه شد.");
            return View(model);
        }

        TempData["Success"] = "پیشنهاد با موفقیت ویرایش شد.";
        return RedirectToAction(nameof(Details), new { id = model.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
        {
            TempData["Error"] = "شناسه پیشنهاد معتبر نیست.";
            return RedirectToAction(nameof(Index));
        }

        var command = new DeleteProductOfferCommand(id);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error ?? "حذف پیشنهاد با خطا مواجه شد.";
        }
        else
        {
            TempData["Success"] = "پیشنهاد با موفقیت حذف شد.";
        }

        return RedirectToAction(nameof(Index));
    }

    private static bool TryConvertPublishedAt(
        ProductOfferFormViewModel model,
        out DateTimeOffset? publishedAt,
        out string? errorMessage)
    {
        publishedAt = null;
        errorMessage = null;

        if (model is null)
        {
            return true;
        }

        var rawDateInput = model.PublishedAtPersian;
        var normalizedDate = NormalizePersianDateInput(rawDateInput);
        var dateProvided = !string.IsNullOrWhiteSpace(rawDateInput);
        model.PublishedAtPersian = normalizedDate;

        var rawTimeInput = model.PublishedAtTime;
        if (!TryNormalizeTimeInput(rawTimeInput, out var hour, out var minute, out var normalizedTime, out var timeError))
        {
            model.PublishedAtTime = normalizedTime;
            errorMessage = timeError ?? "ساعت انتشار وارد شده معتبر نیست.";
            return false;
        }

        model.PublishedAtTime = normalizedTime;

        if (!model.IsPublished)
        {
            return true;
        }

        if (string.IsNullOrEmpty(normalizedDate))
        {
            if (dateProvided || !string.IsNullOrWhiteSpace(rawTimeInput))
            {
                errorMessage = "تاریخ انتشار وارد شده معتبر نیست.";
                return false;
            }

            return true;
        }

        if (!TryExtractPersianDateParts(normalizedDate, out var year, out var month, out var day))
        {
            errorMessage = "تاریخ انتشار وارد شده معتبر نیست.";
            return false;
        }

        global::PersianDateTime persianDateTime;

        try
        {
            persianDateTime = new global::PersianDateTime(year, month, day, hour, minute, 0);
        }
        catch (ArgumentOutOfRangeException)
        {
            errorMessage = "تاریخ انتشار وارد شده معتبر نیست.";
            return false;
        }
        catch (Exception)
        {
            errorMessage = "تاریخ انتشار وارد شده معتبر نیست.";
            return false;
        }

        var gregorian = persianDateTime.ToDateTime();
        var offset = GetIranOffset(gregorian);
        publishedAt = new DateTimeOffset(DateTime.SpecifyKind(gregorian, DateTimeKind.Unspecified), offset);

        return true;
    }

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
                continue;
            }
        }

        return TimeSpan.FromHours(3.5);
    }
}


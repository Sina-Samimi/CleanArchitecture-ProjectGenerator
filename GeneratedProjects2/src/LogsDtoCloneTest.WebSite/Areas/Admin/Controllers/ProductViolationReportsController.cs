using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using LogsDtoCloneTest.Application.Commands.Catalog;
using LogsDtoCloneTest.Application.Queries.Catalog;
using LogsDtoCloneTest.SharedKernel.Extensions;
using LogsDtoCloneTest.WebSite.Areas.Admin.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace LogsDtoCloneTest.WebSite.Areas.Admin.Controllers;

[Area("Admin")]
public sealed class ProductViolationReportsController : Controller
{
    private readonly IMediator _mediator;

    public ProductViolationReportsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        int page = 1,
        bool? isReviewed = null,
        Guid? productId = null,
        string? sellerId = null,
        string? reporterPhone = null,
        string? subject = null,
        string? dateFrom = null,
        string? dateTo = null,
        CancellationToken cancellationToken = default)
    {
        ViewData["Title"] = "مدیریت گزارش‌های تخلف محصولات";
        ViewData["Subtitle"] = "رسیدگی به گزارش‌های تخلف کاربران";

        var pageNumber = page < 1 ? 1 : page;
        var pageSize = 20;

        // Parse Persian date strings using shared parser (expects yyyy-MM-dd normalized)
        DateTimeOffset? parsedDateFrom = null;
        DateTimeOffset? parsedDateTo = null;

        string? parsedDateFromNormalized = null;
        string? parsedDateToNormalized = null;

        if (!string.IsNullOrWhiteSpace(dateFrom))
        {
            var normalized = dateFrom.Replace('/', '-');
            var parsed = UserFilterFormatting.ParsePersianDate(normalized, toExclusiveEnd: false, out var normalizedOut);
            parsedDateFrom = parsed;
            parsedDateFromNormalized = normalizedOut?.Replace('-', '/');
        }

        if (!string.IsNullOrWhiteSpace(dateTo))
        {
            var normalized = dateTo.Replace('/', '-');
            var parsed = UserFilterFormatting.ParsePersianDate(normalized, toExclusiveEnd: true, out var normalizedOut);
            parsedDateTo = parsed;
            parsedDateToNormalized = normalizedOut?.Replace('-', '/');
        }

        var query = new GetProductViolationReportsQuery(pageNumber, pageSize, productId, sellerId, isReviewed, reporterPhone, subject, parsedDateFrom, parsedDateTo);
        var result = await _mediator.Send(query, cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            TempData["Error"] = result.Error ?? "دریافت لیست گزارش‌های تخلف با خطا مواجه شد.";
            return View(new ProductViolationReportListViewModel
            {
                Reports = Array.Empty<ProductViolationReportViewModel>(),
                TotalCount = 0,
                PageNumber = 1,
                PageSize = pageSize
            });
        }

        var data = result.Value;
        var viewModel = new ProductViolationReportListViewModel
        {
            Reports = data.Reports.Select(r => new ProductViolationReportViewModel
            {
                Id = r.Id,
                ProductId = r.ProductId,
                ProductName = r.ProductName,
                ProductSellerId = r.ProductSellerId,
                ProductOfferId = r.ProductOfferId,
                SellerId = r.SellerId,
                SellerName = r.SellerName,
                Subject = r.Subject,
                Message = r.Message,
                ReporterId = r.ReporterId,
                ReporterPhone = r.ReporterPhone,
                IsReviewed = r.IsReviewed,
                CreatedAt = r.CreatedAt,
                ReviewedAt = r.ReviewedAt,
                ReviewedById = r.ReviewedById
            }).ToArray(),
            TotalCount = data.TotalCount,
            PageNumber = data.PageNumber,
            PageSize = data.PageSize,
            SelectedIsReviewed = isReviewed,
            SelectedProductId = productId,
            SelectedSellerId = sellerId,
            SelectedReporterPhone = reporterPhone,
            SelectedSubject = subject,
            SelectedDateFrom = parsedDateFrom,
            SelectedDateTo = parsedDateTo,
            SelectedDateFromPersian = parsedDateFromNormalized,
            SelectedDateToPersian = parsedDateToNormalized
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAsReviewed(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
        {
            TempData["Error"] = "شناسه گزارش معتبر نیست.";
            return RedirectToAction(nameof(Index));
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            TempData["Error"] = "شناسه کاربری معتبر نیست.";
            return RedirectToAction(nameof(Index));
        }

        var command = new MarkProductViolationReportAsReviewedCommand(id, userId);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error ?? "بروزرسانی وضعیت گزارش با خطا مواجه شد.";
        }
        else
        {
            TempData["Success"] = "گزارش به عنوان بررسی شده علامت‌گذاری شد.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UnmarkAsReviewed(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
        {
            TempData["Error"] = "شناسه گزارش معتبر نیست.";
            return RedirectToAction(nameof(Index));
        }

        var command = new UnmarkProductViolationReportAsReviewedCommand(id);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error ?? "بروزرسانی وضعیت گزارش با خطا مواجه شد.";
        }
        else
        {
            TempData["Success"] = "گزارش به عنوان بررسی نشده علامت‌گذاری شد.";
        }

        return RedirectToAction(nameof(Index));
    }
}


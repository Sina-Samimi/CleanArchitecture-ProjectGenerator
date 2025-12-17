using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LogsDtoCloneTest.Application.Commands.Orders;
using LogsDtoCloneTest.Application.Queries.Orders;
using LogsDtoCloneTest.Domain.Enums;
using LogsDtoCloneTest.SharedKernel.BaseTypes;
using LogsDtoCloneTest.SharedKernel.Extensions;
using LogsDtoCloneTest.WebSite.Areas.Admin.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace LogsDtoCloneTest.WebSite.Areas.Admin.Controllers;

[Area("Admin")]
public sealed class OrdersController : Controller
{
    private readonly IMediator _mediator;

    public OrdersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        [FromQuery] string? searchTerm,
        [FromQuery] string? userId,
        [FromQuery] InvoiceStatus? status,
        [FromQuery] string? fromDate,
        [FromQuery] string? toDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        ViewData["Title"] = "سفارشات";
        ViewData["Subtitle"] = "لیست خریدها و سفارشات کاربران";

        var cancellationToken = HttpContext.RequestAborted;

        DateTimeOffset? fromDateParsed = null;
        DateTimeOffset? toDateParsed = null;

        if (!string.IsNullOrWhiteSpace(fromDate))
        {
            fromDateParsed = UserFilterFormatting.ParsePersianDate(fromDate, toExclusiveEnd: false, out _);
        }

        if (!string.IsNullOrWhiteSpace(toDate))
        {
            toDateParsed = UserFilterFormatting.ParsePersianDate(toDate, toExclusiveEnd: true, out _);
        }

        var query = new GetOrdersQuery(
            SellerId: null,
            UserId: userId,
            Status: status,
            FromDate: fromDateParsed,
            ToDate: toDateParsed,
            PageNumber: page,
            PageSize: pageSize);

        var result = await _mediator.Send(query, cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            TempData["Error"] = result.Error ?? "خطا در دریافت لیست سفارشات.";
            return View(new OrderIndexViewModel
            {
                Orders = Array.Empty<OrderListItemViewModel>(),
                TotalCount = 0,
                PageNumber = page,
                PageSize = pageSize
            });
        }

        var data = result.Value;
        var orders = data.Items.Select(order => new OrderListItemViewModel
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
            SellerId = order.SellerId
        }).ToArray();

        var model = new OrderIndexViewModel
        {
            Orders = orders,
            TotalCount = data.TotalCount,
            PageNumber = data.PageNumber,
            PageSize = data.PageSize,
            TotalPages = data.TotalPages,
            Filter = new OrderFilterViewModel
            {
                SearchTerm = searchTerm,
                UserId = userId,
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

        // Redirect to invoice details
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


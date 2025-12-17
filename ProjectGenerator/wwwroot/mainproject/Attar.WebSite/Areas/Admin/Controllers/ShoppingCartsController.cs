using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Attar.Application.Queries.Cart;
using Attar.SharedKernel.Extensions;
using Attar.WebSite.Areas.Admin.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Attar.WebSite.Areas.Admin.Controllers;

[Area("Admin")]
public sealed class ShoppingCartsController : Controller
{
    private readonly IMediator _mediator;

    public ShoppingCartsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        [FromQuery] string? userId,
        [FromQuery] string? fromDate,
        [FromQuery] string? toDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        ViewData["Title"] = "سبدخرید فعلی";
        ViewData["Subtitle"] = "لیست سبدهای خریدی که هنوز پرداخت نشده‌اند";

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

        var query = new GetActiveCartsQuery(
            UserId: userId,
            FromDate: fromDateParsed,
            ToDate: toDateParsed,
            PageNumber: page,
            PageSize: pageSize);

        var result = await _mediator.Send(query, cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            TempData["Error"] = result.Error ?? "خطا در دریافت لیست سبدهای خرید.";
            return View(new ActiveCartIndexViewModel
            {
                Carts = Array.Empty<ActiveCartListItemViewModel>(),
                TotalCount = 0,
                PageNumber = page,
                PageSize = pageSize
            });
        }

        var data = result.Value;
        var carts = data.Items.Select(cart => new ActiveCartListItemViewModel
        {
            Id = cart.Id,
            UserId = cart.UserId,
            UserFullName = cart.UserFullName,
            UserPhoneNumber = cart.UserPhoneNumber,
            ItemCount = cart.ItemCount,
            Subtotal = cart.Subtotal,
            DiscountTotal = cart.DiscountTotal,
            GrandTotal = cart.GrandTotal,
            UpdateDate = cart.UpdateDate,
            CreateDate = cart.CreateDate
        }).ToArray();

        var model = new ActiveCartIndexViewModel
        {
            Carts = carts,
            TotalCount = data.TotalCount,
            PageNumber = data.PageNumber,
            PageSize = data.PageSize,
            TotalPages = data.TotalPages,
            Filter = new ActiveCartFilterViewModel
            {
                UserId = userId,
                FromDate = fromDate,
                ToDate = toDate
            }
        };

        return View(model);
    }
}


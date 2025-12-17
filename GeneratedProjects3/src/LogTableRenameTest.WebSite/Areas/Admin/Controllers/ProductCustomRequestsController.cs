using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LogTableRenameTest.Application.Commands.Catalog;
using LogTableRenameTest.Application.Queries.Catalog;
using LogTableRenameTest.Domain.Enums;
using LogTableRenameTest.WebSite.Areas.Admin.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace LogTableRenameTest.WebSite.Areas.Admin.Controllers;

[Area("Admin")]
public sealed class ProductCustomRequestsController : Controller
{
    private readonly IMediator _mediator;

    public ProductCustomRequestsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        int page = 1,
        CustomRequestStatus? status = null,
        Guid? productId = null,
        CancellationToken cancellationToken = default)
    {
        ViewData["Title"] = "مدیریت درخواست‌های سفارشی";
        ViewData["Subtitle"] = "رسیدگی به درخواست‌های سفارشی محصولات";

        var pageNumber = page < 1 ? 1 : page;
        var pageSize = 20;

        var query = new GetProductCustomRequestsQuery(pageNumber, pageSize, status, productId);
        var result = await _mediator.Send(query, cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            TempData["Error"] = result.Error ?? "دریافت لیست درخواست‌ها با خطا مواجه شد.";
            return View(new ProductCustomRequestListViewModel
            {
                Requests = Array.Empty<ProductCustomRequestViewModel>(),
                TotalCount = 0,
                PageNumber = 1,
                PageSize = pageSize
            });
        }

        var data = result.Value;
        var viewModel = new ProductCustomRequestListViewModel
        {
            Requests = data.Requests.Select(r => new ProductCustomRequestViewModel
            {
                Id = r.Id,
                ProductId = r.ProductId,
                ProductName = r.ProductName,
                UserId = r.UserId,
                FullName = r.FullName,
                Phone = r.Phone,
                Email = r.Email,
                Message = r.Message,
                Status = r.Status,
                CreatedAt = r.CreatedAt,
                ContactedAt = r.ContactedAt,
                AdminNotes = r.AdminNotes
            }).ToArray(),
            TotalCount = data.TotalCount,
            PageNumber = data.PageNumber,
            PageSize = data.PageSize,
            SelectedStatus = status,
            SelectedProductId = productId
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(
        Guid id,
        CustomRequestStatus status,
        string? adminNotes,
        CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
        {
            TempData["Error"] = "شناسه درخواست معتبر نیست.";
            return RedirectToAction(nameof(Index));
        }

        var command = new UpdateProductCustomRequestStatusCommand(id, status, adminNotes);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error ?? "بروزرسانی وضعیت درخواست با خطا مواجه شد.";
        }
        else
        {
            var statusLabel = status switch
            {
                CustomRequestStatus.Pending => "در انتظار",
                CustomRequestStatus.Contacted => "تماس گرفته شده",
                CustomRequestStatus.Completed => "تکمیل شده",
                CustomRequestStatus.Cancelled => "لغو شده",
                _ => "نامشخص"
            };
            TempData["Success"] = $"وضعیت درخواست به «{statusLabel}» تغییر یافت.";
        }

        return RedirectToAction(nameof(Index));
    }
}


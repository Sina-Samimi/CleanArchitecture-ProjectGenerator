using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using TestAttarClone.Application.Queries.Sellers;
using TestAttarClone.SharedKernel.Authorization;
using TestAttarClone.WebSite.Areas.Seller.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TestAttarClone.WebSite.Areas.Seller.Controllers;

[Area("Seller")]
[Authorize(Policy = AuthorizationPolicies.SellerPanelAccess)]
public sealed class PaymentsController : Controller
{
    private readonly IMediator _mediator;

    public PaymentsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "درآمدها و پرداخت‌ها";
        ViewData["Sidebar:ActiveTab"] = "payments";

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            TempData["Error"] = "کاربر احراز هویت نشده است.";
            return RedirectToAction("Index", "Products");
        }

        var cancellationToken = HttpContext.RequestAborted;
        var query = new GetSellerPaymentsQuery(userId);
        var result = await _mediator.Send(query, cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            TempData["Error"] = result.Error ?? "خطا در دریافت اطلاعات پرداخت‌ها.";
            return View(new SellerPaymentsViewModel
            {
                GeneratedAt = DateTimeOffset.UtcNow
            });
        }

        var payments = result.Value;
        var viewModel = new SellerPaymentsViewModel
        {
            TotalRevenue = payments.TotalRevenue,
            PaidRevenue = payments.PaidRevenue,
            PendingRevenue = payments.PendingRevenue,
            TotalInvoices = payments.TotalInvoices,
            PaidInvoices = payments.PaidInvoices,
            PendingInvoices = payments.PendingInvoices,
            Invoices = payments.Invoices.Select(i => new SellerInvoiceViewModel
            {
                InvoiceId = i.InvoiceId,
                InvoiceNumber = i.InvoiceNumber,
                Title = i.Title,
                TotalAmount = i.TotalAmount,
                PaidAmount = i.PaidAmount,
                PendingAmount = i.PendingAmount,
                Status = i.Status,
                IssueDate = i.IssueDate,
                DueDate = i.DueDate,
                ProductCount = i.ProductCount
            }).ToList(),
            GeneratedAt = payments.GeneratedAt
        };

        return View(viewModel);
    }
}


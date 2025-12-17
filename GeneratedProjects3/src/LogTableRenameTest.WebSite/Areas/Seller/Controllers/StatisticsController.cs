using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using LogTableRenameTest.Application.Queries.Sellers;
using LogTableRenameTest.SharedKernel.Authorization;
using LogTableRenameTest.WebSite.Areas.Seller.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogTableRenameTest.WebSite.Areas.Seller.Controllers;

[Area("Seller")]
[Authorize(Policy = AuthorizationPolicies.SellerPanelAccess)]
public sealed class StatisticsController : Controller
{
    private readonly IMediator _mediator;

    public StatisticsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "آمار و گزارشات";
        ViewData["Sidebar:ActiveTab"] = "statistics";

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            TempData["Error"] = "کاربر احراز هویت نشده است.";
            return RedirectToAction("Index", "Products");
        }

        var cancellationToken = HttpContext.RequestAborted;
        var query = new GetSellerStatisticsQuery(userId);
        var result = await _mediator.Send(query, cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            TempData["Error"] = result.Error ?? "خطا در دریافت آمار.";
            return View(new SellerStatisticsViewModel
            {
                GeneratedAt = DateTimeOffset.UtcNow
            });
        }

        var stats = result.Value;
        var viewModel = new SellerStatisticsViewModel
        {
            TotalProducts = stats.TotalProducts,
            PublishedProducts = stats.PublishedProducts,
            PendingProducts = stats.PendingProducts,
            TotalViews = stats.TotalViews,
            TotalOrders = stats.TotalOrders,
            PaidOrders = stats.PaidOrders,
            PendingOrders = stats.PendingOrders,
            TotalRevenue = stats.TotalRevenue,
            TotalComments = stats.TotalComments,
            ApprovedComments = stats.ApprovedComments,
            PendingReplyComments = stats.PendingReplyComments,
            TotalCustomRequests = stats.TotalCustomRequests,
            PendingCustomRequests = stats.PendingCustomRequests,
            TotalShipments = stats.TotalShipments,
            PreparingShipments = stats.PreparingShipments,
            ShippedShipments = stats.ShippedShipments,
            DeliveredShipments = stats.DeliveredShipments,
            TopProducts = stats.TopProducts.Select(p => new ProductStatisticsViewModel
            {
                ProductId = p.ProductId,
                ProductTitle = p.ProductTitle,
                ViewCount = p.ViewCount,
                IsPublished = p.IsPublished
            }).ToList(),
            RecentOrders = stats.RecentOrders.Select(o => new OrderStatisticsViewModel
            {
                OrderId = o.OrderId,
                InvoiceNumber = o.InvoiceNumber,
                TotalAmount = o.TotalAmount,
                Status = o.Status,
                OrderDate = o.OrderDate
            }).ToList(),
            DailyViews = stats.DailyViews.Select(d => new DailyViewViewModel
            {
                Date = d.Date,
                ViewCount = d.ViewCount
            }).ToList(),
            GeneratedAt = stats.GeneratedAt
        };

        return View(viewModel);
    }
}


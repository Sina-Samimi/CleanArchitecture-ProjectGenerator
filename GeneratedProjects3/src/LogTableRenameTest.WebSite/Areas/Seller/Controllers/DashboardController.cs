using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using LogTableRenameTest.Application.Queries.Sellers;
using LogTableRenameTest.SharedKernel.Authorization;
using LogTableRenameTest.WebSite.Areas.Seller.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace LogTableRenameTest.WebSite.Areas.Seller.Controllers;

[Area("Seller")]
[Authorize(Policy = AuthorizationPolicies.SellerPanelAccess)]
public sealed class DashboardController : Controller
{
    private readonly IMediator _mediator;

    public DashboardController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "داشبورد";
        ViewData["Subtitle"] = "خلاصه گزارش عملکرد شما";
        ViewData["Sidebar:ActiveTab"] = "dashboard";

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            TempData["Error"] = "کاربر احراز هویت نشده است.";
            return RedirectToAction("Index", "Products");
        }

        var cancellationToken = HttpContext.RequestAborted;
        var query = new GetSellerDashboardStatsQuery(userId);
        var result = await _mediator.Send(query, cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            TempData["Error"] = result.Error ?? "خطا در دریافت آمار داشبورد.";
            return View(new SellerDashboardViewModel
            {
                GeneratedAt = DateTimeOffset.UtcNow
            });
        }

        var stats = result.Value;
        var viewModel = new SellerDashboardViewModel
        {
            TotalProducts = stats.TotalProducts,
            PublishedProducts = stats.PublishedProducts,
            PendingProducts = stats.PendingProducts,
            DraftProducts = stats.DraftProducts,
            TotalOrders = stats.TotalOrders,
            NewOrdersCount = stats.NewOrdersCount,
            PaidOrders = stats.PaidOrders,
            PendingOrders = stats.PendingOrders,
            TotalRevenue = stats.TotalRevenue,
            TotalComments = stats.TotalComments,
            PendingReplyComments = stats.PendingReplyComments,
            TotalCustomRequests = stats.TotalCustomRequests,
            PendingCustomRequests = stats.PendingCustomRequests,
            TotalShipments = stats.TotalShipments,
            PreparingShipments = stats.PreparingShipments,
            ShippedShipments = stats.ShippedShipments,
            DeliveredShipments = stats.DeliveredShipments,
            NewProductCommentsCount = stats.NewProductCommentsCount,
            ApprovedProductCommentsCount = stats.ApprovedProductCommentsCount,
                GeneratedAt = stats.GeneratedAt,
                LowStockProducts = stats.LowStockProducts
                    .Select(p => new LowStockProductViewModel
                    {
                        Id = p.Id,
                        Name = p.Name,
                        StockQuantity = p.StockQuantity
                    })
                    .ToList()
        };

        return View(viewModel);
    }
}


using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using TestAttarClone.Application.Queries.Catalog;
using TestAttarClone.WebSite.Areas.Seller.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TestAttarClone.SharedKernel.Authorization;

namespace TestAttarClone.WebSite.Areas.Seller.Controllers;

[Area("Seller")]
[Authorize(Policy = AuthorizationPolicies.SellerPanelAccess)]
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
        CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        ConfigureLayoutContext();
        ViewData["Title"] = "گزارش‌های تخلف محصولات من";
        ViewData["Subtitle"] = "رسیدگی به گزارش‌های تخلف محصولات شما";

        var pageNumber = page < 1 ? 1 : page;
        var pageSize = 20;

        var query = new GetProductViolationReportsQuery(pageNumber, pageSize, null, userId, isReviewed);
        var result = await _mediator.Send(query, cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            TempData["Seller.Error"] = result.Error ?? "دریافت لیست گزارش‌های تخلف با خطا مواجه شد.";
            return View(new SellerProductViolationReportListViewModel
            {
                Reports = Array.Empty<SellerProductViolationReportViewModel>(),
                TotalCount = 0,
                PageNumber = 1,
                PageSize = pageSize
            });
        }

        var data = result.Value;
        var viewModel = new SellerProductViolationReportListViewModel
        {
            Reports = data.Reports.Select(r => new SellerProductViolationReportViewModel
            {
                Id = r.Id,
                ProductId = r.ProductId,
                ProductName = r.ProductName,
                ProductSellerId = r.ProductSellerId,
                ProductOfferId = r.ProductOfferId,
                Subject = r.Subject,
                Message = r.Message,
                ReporterPhone = r.ReporterPhone,
                IsReviewed = r.IsReviewed,
                CreatedAt = r.CreatedAt,
                ReviewedAt = r.ReviewedAt
            }).ToArray(),
            TotalCount = data.TotalCount,
            PageNumber = data.PageNumber,
            PageSize = data.PageSize,
            SelectedIsReviewed = isReviewed
        };

        return View(viewModel);
    }

    private void ConfigureLayoutContext()
    {
        var fullName = User?.FindFirstValue("FullName") ?? User?.Identity?.Name ?? "فروشنده";
        var initial = !string.IsNullOrWhiteSpace(fullName) ? fullName.Trim()[0].ToString() : "ف";
        var email = User?.FindFirstValue(ClaimTypes.Email);
        var phone = User?.FindFirstValue(ClaimTypes.MobilePhone) ?? User?.FindFirstValue("PhoneNumber");

        ViewData["TitleSuffix"] = "پنل فروشنده";
        ViewData["GreetingTitle"] = $"سلام، {fullName} 👋";
        ViewData["GreetingSubtitle"] = "مدیریت درخواست‌های دوره و محصول";
        ViewData["GreetingInitial"] = initial;
        ViewData["AccountName"] = fullName;
        ViewData["AccountInitial"] = initial;
        if (!string.IsNullOrWhiteSpace(email))
        {
            ViewData["AccountEmail"] = email;
        }

        if (!string.IsNullOrWhiteSpace(phone))
        {
            ViewData["AccountPhone"] = phone;
        }

        ViewData["SearchPlaceholder"] = "جستجو در محصولات فروشنده";
        ViewData["ShowSearch"] = false;
        ViewData["Sidebar:ActiveTab"] = "violation-reports";
    }
}


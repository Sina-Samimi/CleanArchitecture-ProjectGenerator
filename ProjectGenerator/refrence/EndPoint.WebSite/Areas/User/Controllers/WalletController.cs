using System;
using System.Linq;
using System.Threading.Tasks;
using Arsis.Application.Commands.Billing;
using Arsis.Application.Commands.Billing.Wallet;
using Arsis.Application.DTOs.Billing;
using Arsis.Application.Queries.Billing;
using Arsis.Domain.Entities;
using Arsis.Domain.Enums;
using Arsis.SharedKernel.Extensions;
using EndPoint.WebSite.Areas.User.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace EndPoint.WebSite.Areas.User.Controllers;

[Area("User")]
[Authorize]
public sealed class WalletController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IMediator _mediator;
    private readonly ILogger<WalletController> _logger;

    public WalletController(UserManager<ApplicationUser> userManager, IMediator mediator, ILogger<WalletController> logger)
    {
        _userManager = userManager;
        _mediator = mediator;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        var dashboardResult = await _mediator.Send(new GetWalletDashboardQuery(user.Id));
        if (!dashboardResult.IsSuccess)
        {
            TempData["Error"] = dashboardResult.Error ?? "امکان دریافت اطلاعات کیف پول وجود ندارد.";
            var fallback = BuildEmptyViewModel(user);
            PrepareLayoutMetadata(user);
            ViewData["Title"] = "مدیریت کیف پول";
            ViewData["Sidebar:ActiveTab"] = "wallet";
            return View(fallback);
        }

        var viewModel = MapToViewModel(user, dashboardResult.Value ?? CreateEmptyDashboardDto(), new ChargeWalletInputModel());
        PrepareLayoutMetadata(user);
        ViewData["Title"] = "مدیریت کیف پول";
        ViewData["Sidebar:ActiveTab"] = "wallet";
        ViewData["Subtitle"] = "شارژ کیف پول، بررسی تراکنش‌ها و پرداخت فاکتورها";
        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Charge([Bind(Prefix = "Charge")] ChargeWalletInputModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        if (!ModelState.IsValid)
        {
            var invalidDashboard = await BuildDashboardAsync(user, model);
            PrepareLayoutMetadata(user);
            ViewData["Title"] = "مدیریت کیف پول";
            ViewData["Sidebar:ActiveTab"] = "wallet";
            return View("Index", invalidDashboard);
        }

        var command = new ChargeWalletCommand(user.Id, model.Amount, model.Currency, model.Description);
        var result = await _mediator.Send(command);
        if (!result.IsSuccess)
        {
            _logger.LogWarning("Charging wallet for user {UserId} failed: {Error}", user.Id, result.Error);
            ModelState.AddModelError(string.Empty, result.Error ?? "فرایند شارژ کیف پول با خطا مواجه شد.");
            var failedDashboard = await BuildDashboardAsync(user, model);
            PrepareLayoutMetadata(user);
            ViewData["Title"] = "مدیریت کیف پول";
            ViewData["Sidebar:ActiveTab"] = "wallet";
            return View("Index", failedDashboard);
        }

        TempData["Success"] = "کیف پول شما با موفقیت شارژ شد.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> PayInvoice(Guid id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        if (id == Guid.Empty)
        {
            TempData["Error"] = "شناسه فاکتور معتبر نیست.";
            return RedirectToAction(nameof(Index));
        }

        var options = await BuildPaymentOptionsAsync(user, id);
        if (options is null)
        {
            TempData["Error"] = "فاکتور مورد نظر یافت نشد.";
            return RedirectToAction(nameof(Index));
        }

        PrepareLayoutMetadata(user);
        ViewData["Title"] = $"انتخاب روش پرداخت برای فاکتور {options.InvoiceNumber}";
        ViewData["Sidebar:ActiveTab"] = "wallet";
        ViewData["Subtitle"] = "روش پرداخت مورد نظر خود را انتخاب کنید.";
        return View(options);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PayInvoice(Guid invoiceId, PaymentMethod method)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        if (invoiceId == Guid.Empty)
        {
            TempData["Error"] = "شناسه فاکتور معتبر نیست.";
            return RedirectToAction(nameof(Index));
        }

        var invoiceResult = await _mediator.Send(new GetUserInvoiceDetailsQuery(invoiceId, user.Id));
        if (!invoiceResult.IsSuccess || invoiceResult.Value is null)
        {
            TempData["Error"] = invoiceResult.Error ?? "فاکتور مورد نظر یافت نشد.";
            return RedirectToAction(nameof(Index));
        }

        var paymentResult = await _mediator.Send(new PayInvoiceCommand(invoiceId, user.Id, method));
        if (!paymentResult.IsSuccess || paymentResult.Value is null)
        {
            _logger.LogWarning(
                "Invoice payment initiation failed for user {UserId}, invoice {InvoiceId} via {Method}: {Error}",
                user.Id,
                invoiceId,
                method,
                paymentResult.Error);

            ModelState.AddModelError(string.Empty, paymentResult.Error ?? "پرداخت فاکتور با خطا مواجه شد.");

            var options = await BuildPaymentOptionsAsync(user, invoiceId);
            if (options is null)
            {
                TempData["Error"] = paymentResult.Error ?? "امکان نمایش گزینه‌های پرداخت وجود ندارد.";
                return RedirectToAction(nameof(Index));
            }

            PrepareLayoutMetadata(user);
            ViewData["Title"] = $"انتخاب روش پرداخت برای فاکتور {options.InvoiceNumber}";
            ViewData["Sidebar:ActiveTab"] = "wallet";
            ViewData["Subtitle"] = "روش پرداخت مورد نظر خود را انتخاب کنید.";
            return View(options);
        }

        var payment = paymentResult.Value;

        if (payment.Method == PaymentMethod.Wallet && payment.WalletTransaction is not null)
        {
            TempData["Success"] = "فاکتور با موفقیت از طریق کیف پول تسویه شد.";
            return RedirectToAction(nameof(InvoiceDetails), new { id = invoiceId });
        }

        if (payment.Method == PaymentMethod.OnlineGateway && payment.BankSession is not null)
        {
            var sessionViewModel = MapToBankPaymentSessionViewModel(invoiceResult.Value, payment.BankSession);
            PrepareLayoutMetadata(user);
            ViewData["Title"] = "اتصال به درگاه بانکی";
            ViewData["Sidebar:ActiveTab"] = "wallet";
            ViewData["Subtitle"] = "برای تکمیل پرداخت به درگاه بانکی منتقل شوید.";
            return View("BankPaymentSession", sessionViewModel);
        }

        TempData["Error"] = "پاسخ پرداخت نامعتبر است.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmBankPayment(Guid invoiceId, string reference)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        if (invoiceId == Guid.Empty || string.IsNullOrWhiteSpace(reference))
        {
            TempData["Error"] = "اطلاعات تایید پرداخت معتبر نیست.";
            return RedirectToAction(nameof(Index));
        }

        var invoiceResult = await _mediator.Send(new GetUserInvoiceDetailsQuery(invoiceId, user.Id));
        if (!invoiceResult.IsSuccess || invoiceResult.Value is null)
        {
            TempData["Error"] = invoiceResult.Error ?? "فاکتور مورد نظر یافت نشد.";
            return RedirectToAction(nameof(Index));
        }

        var confirmationResult = await _mediator.Send(new ConfirmBankInvoicePaymentCommand(invoiceId, reference));
        if (!confirmationResult.IsSuccess || confirmationResult.Value is null)
        {
            _logger.LogWarning(
                "Bank payment confirmation failed for user {UserId}, invoice {InvoiceId} with reference {Reference}: {Error}",
                user.Id,
                invoiceId,
                reference,
                confirmationResult.Error);

            TempData["Error"] = confirmationResult.Error ?? "تایید پرداخت بانکی با خطا مواجه شد.";
            return RedirectToAction(nameof(PayInvoice), new { id = invoiceId });
        }

        TempData["Success"] = "پرداخت بانکی با موفقیت تایید شد.";
        return RedirectToAction(nameof(InvoiceDetails), new { id = invoiceId });
    }

    [HttpGet]
    public async Task<IActionResult> InvoiceDetails(Guid id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        if (id == Guid.Empty)
        {
            TempData["Error"] = "شناسه فاکتور معتبر نیست.";
            return RedirectToAction(nameof(Index));
        }

        var result = await _mediator.Send(new GetUserInvoiceDetailsQuery(id, user.Id));
        if (!result.IsSuccess || result.Value is null)
        {
            TempData["Error"] = result.Error ?? "فاکتور مورد نظر یافت نشد.";
            return RedirectToAction(nameof(Index));
        }

        var viewModel = MapToInvoiceDetailViewModel(result.Value);
        PrepareLayoutMetadata(user);
        ViewData["Title"] = $"جزئیات فاکتور {viewModel.InvoiceNumber}";
        ViewData["Sidebar:ActiveTab"] = "wallet";
        ViewData["Subtitle"] = "جزئیات کامل فاکتور و تراکنش‌های مرتبط";
        return View(viewModel);
    }

    private async Task<WalletDashboardViewModel> BuildDashboardAsync(ApplicationUser user, ChargeWalletInputModel chargeModel)
    {
        var dashboardResult = await _mediator.Send(new GetWalletDashboardQuery(user.Id));
        var dto = dashboardResult.IsSuccess && dashboardResult.Value is not null
            ? dashboardResult.Value
            : CreateEmptyDashboardDto();

        return MapToViewModel(user, dto, chargeModel);
    }

    private static WalletDashboardViewModel MapToViewModel(ApplicationUser user, WalletDashboardDto dto, ChargeWalletInputModel chargeModel)
    {
        var transactions = dto.Transactions
            .Select(transaction => new WalletTransactionViewModel
            {
                Id = transaction.Id,
                Amount = transaction.Amount,
                Type = transaction.Type.GetDisplayName(),
                Status = transaction.Status.GetDisplayName(),
                BalanceAfter = transaction.BalanceAfter,
                Reference = transaction.Reference,
                Description = transaction.Description,
                InvoiceId = transaction.InvoiceId,
                OccurredAt = transaction.OccurredAt
            })
            .ToArray();

        var invoices = dto.Invoices
            .Select(invoice => new WalletInvoiceViewModel
            {
                Id = invoice.Id,
                InvoiceNumber = invoice.InvoiceNumber,
                Title = invoice.Title,
                Status = invoice.Status.GetDisplayName(),
                GrandTotal = invoice.GrandTotal,
                OutstandingAmount = invoice.OutstandingAmount,
                IssueDate = invoice.IssueDate
            })
            .ToArray();

        WalletCartViewModel? cartViewModel = null;
        if (dto.Cart is not null)
        {
            var items = dto.Cart.Items
                .Select(item => new WalletCartItemViewModel
                {
                    ProductId = item.ProductId,
                    ProductName = item.ProductName,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    LineTotal = item.LineTotal,
                    ThumbnailPath = item.ThumbnailPath,
                    ProductType = item.ProductType.GetDisplayName()
                })
                .ToArray();

            cartViewModel = new WalletCartViewModel
            {
                Id = dto.Cart.Id,
                ItemCount = dto.Cart.ItemCount,
                Subtotal = dto.Cart.Subtotal,
                DiscountTotal = dto.Cart.DiscountTotal,
                GrandTotal = dto.Cart.GrandTotal,
                UpdatedAt = dto.Cart.UpdatedAt,
                Items = items
            };
        }

        return new WalletDashboardViewModel
        {
            Summary = new WalletSummaryViewModel
            {
                Balance = dto.Summary.Balance,
                Currency = dto.Summary.Currency,
                IsLocked = dto.Summary.IsLocked,
                LastActivityOn = dto.Summary.LastActivityOn
            },
            Transactions = transactions,
            Invoices = invoices,
            Cart = cartViewModel,
            Charge = new ChargeWalletInputModel
            {
                Amount = chargeModel.Amount,
                Description = chargeModel.Description,
                Currency = string.IsNullOrWhiteSpace(chargeModel.Currency) ? dto.Summary.Currency : chargeModel.Currency
            }
        };
    }

    private static UserInvoiceDetailViewModel MapToInvoiceDetailViewModel(InvoiceDetailDto dto)
    {
        var itemViewModels = dto.Items
            .Select(item => new UserInvoiceItemViewModel
            {
                Id = item.Id,
                Name = item.Name,
                Description = item.Description,
                ItemType = item.ItemType.GetDisplayName(),
                ReferenceId = item.ReferenceId,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                DiscountAmount = item.DiscountAmount,
                Subtotal = item.Subtotal,
                Total = item.Total,
                Attributes = item.Attributes
                    .Select(attribute => new UserInvoiceItemAttributeViewModel
                    {
                        Key = attribute.Key,
                        Value = attribute.Value
                    })
                    .ToArray()
            })
            .ToArray();

        var transactionViewModels = dto.Transactions
            .Select(transaction => new UserInvoiceTransactionViewModel
            {
                Id = transaction.Id,
                Amount = transaction.Amount,
                Method = transaction.Method.GetDisplayName(),
                Status = transaction.Status.GetDisplayName(),
                Reference = transaction.Reference,
                GatewayName = transaction.GatewayName,
                Description = transaction.Description,
                Metadata = transaction.Metadata,
                OccurredAt = transaction.OccurredAt
            })
            .ToArray();

        return new UserInvoiceDetailViewModel
        {
            Id = dto.Id,
            InvoiceNumber = dto.InvoiceNumber,
            Title = dto.Title,
            Description = dto.Description,
            Status = dto.Status.GetDisplayName(),
            Currency = dto.Currency,
            Subtotal = dto.Subtotal,
            DiscountTotal = dto.DiscountTotal,
            TaxAmount = dto.TaxAmount,
            AdjustmentAmount = dto.AdjustmentAmount,
            GrandTotal = dto.GrandTotal,
            PaidAmount = dto.PaidAmount,
            OutstandingAmount = dto.OutstandingAmount,
            IssueDate = dto.IssueDate,
            DueDate = dto.DueDate,
            ExternalReference = dto.ExternalReference,
            Items = itemViewModels,
            Transactions = transactionViewModels
        };
    }

    private async Task<InvoicePaymentOptionsViewModel?> BuildPaymentOptionsAsync(ApplicationUser user, Guid invoiceId)
    {
        var invoiceResult = await _mediator.Send(new GetUserInvoiceDetailsQuery(invoiceId, user.Id));
        if (!invoiceResult.IsSuccess || invoiceResult.Value is null)
        {
            return null;
        }

        var dashboardResult = await _mediator.Send(new GetWalletDashboardQuery(user.Id));
        var summary = dashboardResult.IsSuccess && dashboardResult.Value is not null
            ? dashboardResult.Value.Summary
            : new WalletSummaryDto(0m, "IRT", false, DateTimeOffset.UtcNow);

        return MapToPaymentOptionsViewModel(invoiceResult.Value, summary);
    }

    private static InvoicePaymentOptionsViewModel MapToPaymentOptionsViewModel(InvoiceDetailDto invoice, WalletSummaryDto summary)
    {
        var currency = string.IsNullOrWhiteSpace(invoice.Currency) ? summary.Currency : invoice.Currency;
        var outstanding = invoice.OutstandingAmount;

        return new InvoicePaymentOptionsViewModel
        {
            InvoiceId = invoice.Id,
            InvoiceNumber = invoice.InvoiceNumber,
            Title = invoice.Title,
            Status = invoice.Status.GetDisplayName(),
            Currency = currency,
            GrandTotal = invoice.GrandTotal,
            PaidAmount = invoice.PaidAmount,
            OutstandingAmount = outstanding,
            IssueDate = invoice.IssueDate,
            DueDate = invoice.DueDate,
            WalletBalance = summary.Balance,
            IsWalletLocked = summary.IsLocked,
            WalletCanCover = !summary.IsLocked && outstanding > 0 && summary.Balance >= outstanding
        };
    }

    private static BankPaymentSessionViewModel MapToBankPaymentSessionViewModel(InvoiceDetailDto invoice, BankPaymentSessionDto session)
        => new()
        {
            InvoiceId = invoice.Id,
            InvoiceNumber = invoice.InvoiceNumber,
            Title = invoice.Title,
            Amount = session.Amount,
            Currency = session.Currency,
            GatewayName = session.GatewayName,
            Reference = session.Reference,
            PaymentUrl = session.PaymentUrl.ToString(),
            ExpiresAt = session.ExpiresAt,
            Description = session.Description
        };

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
        ViewData["GreetingSubtitle"] = "مدیریت مالی حساب کاربری";
        ViewData["GreetingTitle"] = $"سلام، {displayName} 💳";
        ViewData["AccountAvatarUrl"] = user.AvatarPath;
        ViewData["Sidebar:Completion"] = 100;
    }

    private WalletDashboardViewModel BuildEmptyViewModel(ApplicationUser user)
        => MapToViewModel(user, CreateEmptyDashboardDto(), new ChargeWalletInputModel());

    private static WalletDashboardDto CreateEmptyDashboardDto()
        => new(
            new WalletSummaryDto(0m, "IRT", false, DateTimeOffset.UtcNow),
            Array.Empty<WalletTransactionListItemDto>(),
            Array.Empty<WalletInvoiceSnapshotDto>(),
            null,
            DateTimeOffset.UtcNow);
}

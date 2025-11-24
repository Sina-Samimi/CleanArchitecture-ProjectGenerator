using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Arsis.Application.Commands.Billing;
using Arsis.Application.Commands.Billing.Wallet;
using Arsis.Application.Commands.Cart;
using Arsis.Application.Queries.Cart;
using Arsis.Application.Queries.Tests;
using Arsis.Domain.Enums;
using EndPoint.WebSite.Models.Cart;
using EndPoint.WebSite.Services.Cart;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EndPoint.WebSite.Controllers;

[Authorize]
public sealed class CheckoutController : Controller
{
    private const string CheckoutSuccessTempDataKey = "Checkout.Success";
    private const string CheckoutErrorTempDataKey = "Checkout.Error";

    private readonly IMediator _mediator;
    private readonly ICartCookieService _cartCookieService;

    public CheckoutController(
        IMediator mediator,
        ICartCookieService cartCookieService)
    {
        _mediator = mediator;
        _cartCookieService = cartCookieService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var anonymousId = _cartCookieService.GetAnonymousCartId();

        var result = await _mediator.Send(new GetCartQuery(userId, anonymousId), cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            TempData["Error"] = result.Error ?? "سبد خرید شما خالی است.";
            return RedirectToAction("Index", "Cart");
        }

        var cart = result.Value;
        if (cart.Items.Count == 0)
        {
            TempData["Error"] = "سبد خرید شما خالی است.";
            return RedirectToAction("Index", "Cart");
        }

        var model = new CheckoutViewModel
        {
            Cart = CartViewModelFactory.FromDto(cart)
        };

        return View("Index", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ProcessPayment(string paymentMethod, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return RedirectToAction("PhoneLogin", "Account", new { returnUrl = Url.Action("Index", "Checkout") });
        }

        var anonymousId = _cartCookieService.GetAnonymousCartId();
        
        // Verify cart is not empty
        var cartResult = await _mediator.Send(new GetCartQuery(userId, anonymousId), cancellationToken);
        if (!cartResult.IsSuccess || cartResult.Value is null || cartResult.Value.Items.Count == 0)
        {
            TempData[CheckoutErrorTempDataKey] = "سبد خرید شما خالی است.";
            return RedirectToAction("Index", "Cart");
        }

        // Create invoice from cart (this also clears the cart)
        var checkoutResult = await _mediator.Send(new CheckoutCartCommand(userId, anonymousId), cancellationToken);
        if (!checkoutResult.IsSuccess)
        {
            TempData[CheckoutErrorTempDataKey] = checkoutResult.Error ?? "خطا در ایجاد فاکتور.";
            return RedirectToAction("Index", "Cart");
        }

        var invoiceId = checkoutResult.Value;

        // Clear cart cookie
        _cartCookieService.ClearAnonymousCartId();

        // Process payment based on selected method
        if (string.Equals(paymentMethod, "Wallet", StringComparison.OrdinalIgnoreCase))
        {
            // Pay with wallet
            var paymentResult = await _mediator.Send(new PayInvoiceWithWalletCommand(invoiceId, userId), cancellationToken);
            
            if (!paymentResult.IsSuccess)
            {
                TempData[CheckoutErrorTempDataKey] = paymentResult.Error ?? "خطا در پرداخت از کیف پول.";
                ShowPaymentFailedAlert(paymentResult.Error ?? "خطا در پرداخت از کیف پول.");
                return RedirectToAction("Details", "Invoice", new { area = "User", id = invoiceId });
            }

            TempData[CheckoutSuccessTempDataKey] = "پرداخت با موفقیت از کیف پول شما انجام شد.";
            ShowPaymentSuccessAlert();
            return RedirectToAction("Details", "Invoice", new { area = "User", id = invoiceId });
        }
        else if (string.Equals(paymentMethod, "Gateway", StringComparison.OrdinalIgnoreCase))
        {
            // Redirect to payment gateway
            // TODO: Implement payment gateway integration
            TempData[CheckoutErrorTempDataKey] = "پرداخت از طریق درگاه بانکی در حال حاضر در دسترس نیست.";
            ShowPaymentFailedAlert("پرداخت از طریق درگاه بانکی در حال حاضر در دسترس نیست.");
            return RedirectToAction("Details", "Invoice", new { area = "User", id = invoiceId });
        }
        else
        {
            TempData[CheckoutErrorTempDataKey] = "روش پرداخت انتخاب شده معتبر نیست.";
            return RedirectToAction("Index");
        }
    }

    private void ShowPaymentSuccessAlert()
    {
        TempData["Alert.Title"] = "پرداخت موفق";
        TempData["Alert.Message"] = "پرداخت شما با موفقیت انجام شد. فاکتور شما آماده مشاهده است.";
        TempData["Alert.Type"] = "success";
        TempData["Alert.ConfirmText"] = "مشاهده فاکتور";
    }

    [HttpGet]
    public async Task<IActionResult> Test(Guid testId, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return RedirectToAction("PhoneLogin", "Account", new { returnUrl = Url.Action("Test", "Checkout", new { testId }) });
        }

        // Get test details
        var testResult = await _mediator.Send(new GetTestByIdQuery(testId, userId), cancellationToken);
        if (!testResult.IsSuccess || testResult.Value is null)
        {
            TempData["Error"] = testResult.Error ?? "تست مورد نظر یافت نشد.";
            return RedirectToAction("Index", "Test");
        }

        var test = testResult.Value;

        // Check if test is available
        if (!test.IsAvailable)
        {
            TempData["Error"] = "این تست در حال حاضر در دسترس نیست.";
            return RedirectToAction("Details", "Test", new { id = testId });
        }

        // Check if test is free
        if (test.Price == 0)
        {
            // Free test - redirect directly to start
            return RedirectToAction("Start", "Test", new { id = testId });
        }

        // For paid tests, allow multiple attempts with different invoices
        // Skip the CanUserAttempt check for paid tests - user can attempt multiple times with different invoices

        // Create invoice for test
        var invoiceCommand = new CreateInvoiceCommand(
            InvoiceNumber: null,
            Title: $"فاکتور تست: {test.Title}",
            Description: $"پرداخت هزینه تست {test.Title}",
            Currency: string.IsNullOrWhiteSpace(test.Currency) ? "IRT" : test.Currency,
            UserId: userId,
            IssueDate: DateTimeOffset.UtcNow,
            DueDate: DateTimeOffset.UtcNow.AddDays(7),
            TaxAmount: 0,
            AdjustmentAmount: 0,
            ExternalReference: null,
            Items: new[]
            {
                new CreateInvoiceCommand.Item(
                    Name: test.Title,
                    Description: $"تست {GetTestTypeName(test.Type)}",
                    ItemType: InvoiceItemType.Test,
                    ReferenceId: test.Id,
                    Quantity: 1,
                    UnitPrice: test.Price,
                    DiscountAmount: null,
                    Attributes: null)
            });

        var invoiceResult = await _mediator.Send(invoiceCommand, cancellationToken);
        if (!invoiceResult.IsSuccess)
        {
            TempData["Error"] = invoiceResult.Error ?? "خطا در ایجاد فاکتور.";
            return RedirectToAction("Details", "Test", new { id = testId });
        }

        var invoiceId = invoiceResult.Value;

        // Redirect to payment page
        return RedirectToAction("Details", "Invoice", new { area = "User", id = invoiceId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PayTest(Guid testId, Guid invoiceId, string paymentMethod, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return RedirectToAction("PhoneLogin", "Account", new { returnUrl = Url.Action("Test", "Checkout", new { testId }) });
        }

        // Process payment based on selected method
        if (string.Equals(paymentMethod, "Wallet", StringComparison.OrdinalIgnoreCase))
        {
            // Pay with wallet
            var paymentResult = await _mediator.Send(new PayInvoiceWithWalletCommand(invoiceId, userId), cancellationToken);
            
            if (!paymentResult.IsSuccess)
            {
                TempData[CheckoutErrorTempDataKey] = paymentResult.Error ?? "خطا در پرداخت از کیف پول.";
                ShowPaymentFailedAlert(paymentResult.Error ?? "خطا در پرداخت از کیف پول.");
                return RedirectToAction("Details", "Invoice", new { area = "User", id = invoiceId });
            }

            TempData[CheckoutSuccessTempDataKey] = "پرداخت با موفقیت انجام شد. اکنون می‌توانید تست را شروع کنید.";
            ShowPaymentSuccessAlert();
            
            // Redirect to start test with invoice ID
            return RedirectToAction("Start", "Test", new { id = testId, invoiceId });
        }
        else if (string.Equals(paymentMethod, "Gateway", StringComparison.OrdinalIgnoreCase))
        {
            // Redirect to payment gateway
            // TODO: Implement payment gateway integration
            TempData[CheckoutErrorTempDataKey] = "پرداخت از طریق درگاه بانکی در حال حاضر در دسترس نیست.";
            ShowPaymentFailedAlert("پرداخت از طریق درگاه بانکی در حال حاضر در دسترس نیست.");
            return RedirectToAction("Details", "Invoice", new { area = "User", id = invoiceId });
        }
        else
        {
            TempData[CheckoutErrorTempDataKey] = "روش پرداخت انتخاب شده معتبر نیست.";
            return RedirectToAction("Test", new { testId });
        }
    }

    private static string GetTestTypeName(TestType type) => type switch
    {
        TestType.General => "تست عمومی",
        TestType.Disc => "تست DISC",
        TestType.Clifton => "تست کلیفتون",
        TestType.CliftonSchwartz => "تست کلیفتون + شوارتز",
        TestType.Raven => "تست هوش ریون",
        TestType.Personality => "تست شخصیت‌شناسی",
        _ => type.ToString()
    };

    private void ShowPaymentFailedAlert(string error)
    {
        TempData["Alert.Title"] = "پرداخت ناموفق";
        TempData["Alert.Message"] = error;
        TempData["Alert.Type"] = "danger";
        TempData["Alert.ConfirmText"] = "متوجه شدم";
    }

    private string? GetUserId()
        => User?.Identity?.IsAuthenticated == true
            ? User.FindFirstValue(ClaimTypes.NameIdentifier)
            : null;
}

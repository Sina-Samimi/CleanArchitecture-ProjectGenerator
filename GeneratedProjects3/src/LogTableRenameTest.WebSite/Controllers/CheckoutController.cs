using System;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LogTableRenameTest.Application.Commands.Billing;
using LogTableRenameTest.Application.Commands.Billing.Wallet;
using LogTableRenameTest.Application.Commands.Cart;
using LogTableRenameTest.Application.Commands.UserAddresses;
using LogTableRenameTest.Application.Queries.Cart;
using LogTableRenameTest.Application.Queries.UserAddresses;
using LogTableRenameTest.WebSite.Models.Cart;
using LogTableRenameTest.WebSite.Services.Cart;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LogTableRenameTest.SharedKernel.Helpers;

namespace LogTableRenameTest.WebSite.Controllers;

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

        // Load user addresses
        var addressesResult = await _mediator.Send(new GetUserAddressesQuery(userId), cancellationToken);
        var addresses = addressesResult.IsSuccess ? addressesResult.Value : null;

        var model = new CheckoutViewModel
        {
            Cart = CartViewModelFactory.FromDto(cart),
            Addresses = addresses
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

        // Get selected address ID from form
        var selectedAddressId = Request.Form["SelectedAddressId"].ToString();
        Guid? addressId = null;
        if (!string.IsNullOrWhiteSpace(selectedAddressId) && Guid.TryParse(selectedAddressId, out var parsedId))
        {
            addressId = parsedId;
        }

        // Create invoice from cart (this also clears the cart)
        var checkoutResult = await _mediator.Send(new CheckoutCartCommand(userId, anonymousId, addressId), cancellationToken);
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
            // Redirect to payment gateway flow (handled by PaymentController)
            return RedirectToAction("Payment", "Payment", new { orderId = invoiceId });
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





    

    private void ShowPaymentFailedAlert(string error)
    {
        TempData["Alert.Title"] = "پرداخت ناموفق";
        TempData["Alert.Message"] = error;
        TempData["Alert.Type"] = "danger";
        TempData["Alert.ConfirmText"] = "متوجه شدم";
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> AddAddress(
        [FromBody] AddAddressRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Json(new { success = false, error = "کاربر احراز هویت نشده است." });
        }

        if (string.IsNullOrWhiteSpace(request.Title) ||
            string.IsNullOrWhiteSpace(request.RecipientName) ||
            string.IsNullOrWhiteSpace(request.RecipientPhone) ||
            string.IsNullOrWhiteSpace(request.Province) ||
            string.IsNullOrWhiteSpace(request.City) ||
            string.IsNullOrWhiteSpace(request.PostalCode) ||
            string.IsNullOrWhiteSpace(request.AddressLine))
        {
            return Json(new { success = false, error = "لطفاً تمام فیلدهای الزامی را پر کنید." });
        }

        // Validate Iranian mobile phone number
        var normalizedPhone = NormalizePhoneNumber(request.RecipientPhone);
        if (string.IsNullOrWhiteSpace(normalizedPhone) || normalizedPhone.Length != 11 || !normalizedPhone.StartsWith("09", StringComparison.Ordinal))
        {
            return Json(new { success = false, error = "شماره موبایل باید یک شماره موبایل معتبر ایرانی باشد (مثال: 09123456789)." });
        }

        var command = new CreateUserAddressCommand(
            userId,
            request.Title,
            request.RecipientName,
            normalizedPhone,
            request.Province,
            request.City,
            request.PostalCode,
            request.AddressLine,
            request.Plaque,
            request.Unit,
            request.IsDefault);

        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            return Json(new { success = false, error = result.Error ?? "خطا در افزودن آدرس." });
        }

        // Get updated addresses list
        var addressesResult = await _mediator.Send(new GetUserAddressesQuery(userId), cancellationToken);
        var addresses = addressesResult.IsSuccess ? addressesResult.Value : null;

        return Json(new
        {
            success = true,
            addressId = result.Value,
            addresses = addresses?.Select(a => new
            {
                id = a.Id,
                title = a.Title,
                recipientName = a.RecipientName,
                recipientPhone = a.RecipientPhone,
                province = a.Province,
                city = a.City,
                postalCode = a.PostalCode,
                addressLine = a.AddressLine,
                plaque = a.Plaque,
                unit = a.Unit,
                isDefault = a.IsDefault
            })
        });
    }

    public sealed class AddAddressRequest
    {
        public string Title { get; set; } = string.Empty;
        public string RecipientName { get; set; } = string.Empty;
        public string RecipientPhone { get; set; } = string.Empty;
        public string Province { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string PostalCode { get; set; } = string.Empty;
        public string AddressLine { get; set; } = string.Empty;
        public string? Plaque { get; set; }
        public string? Unit { get; set; }
        public bool IsDefault { get; set; }
    }

    private string? GetUserId()
        => User?.Identity?.IsAuthenticated == true
            ? User.FindFirstValue(ClaimTypes.NameIdentifier)
            : null;

    private static string NormalizePhoneNumber(string? phoneNumber)
    {
        var digits = PhoneNumberHelper.ExtractDigits(phoneNumber);
        if (string.IsNullOrWhiteSpace(digits))
        {
            return string.Empty;
        }

        var normalized = digits;
        if (normalized.StartsWith("0098", StringComparison.Ordinal) && normalized.Length >= 5)
        {
            normalized = "0" + normalized[4..];
        }
        else if (normalized.StartsWith("98", StringComparison.Ordinal) && normalized.Length >= 3)
        {
            normalized = "0" + normalized[2..];
        }
        else if (normalized.StartsWith("9", StringComparison.Ordinal) && normalized.Length == 10)
        {
            normalized = string.Concat("0", normalized);
        }

        return normalized;
    }
}

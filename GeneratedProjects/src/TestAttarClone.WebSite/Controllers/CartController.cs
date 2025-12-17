using System;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TestAttarClone.Application.DTOs.Cart;
using TestAttarClone.Application.Commands.Cart;
using TestAttarClone.Application.Queries.Cart;
using TestAttarClone.WebSite.Models.Cart;
using TestAttarClone.WebSite.Services.Cart;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;

namespace TestAttarClone.WebSite.Controllers;

[AllowAnonymous]
public sealed class CartController : Controller
{
    private const string CartSuccessTempDataKey = "Cart.Success";
    private const string CartErrorTempDataKey = "Cart.Error";
    private const string CartDiscountCodeTempDataKey = "Cart.Discount.Code";

    private readonly IMediator _mediator;
    private readonly ICartCookieService _cartCookieService;
    private readonly ILogger<CartController> _logger;
    
    public CartController(
        IMediator mediator,
        ICartCookieService cartCookieService,
        ILogger<CartController> logger)
    {
        _mediator = mediator;
        _cartCookieService = cartCookieService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var anonymousId = _cartCookieService.GetAnonymousCartId();

        var result = await _mediator.Send(new GetCartQuery(userId, anonymousId), cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            TempData[CartErrorTempDataKey] = result.Error ?? "امکان دریافت سبد خرید وجود ندارد.";
            return View("Index", CartViewModelFactory.FromDto(CartDtoMapper.CreateEmpty(anonymousId, userId)));
        }

        var dto = result.Value;
        UpdateCartCookie(dto.AnonymousId);

        var model = CartViewModelFactory.FromDto(dto);
        ViewBag.SuccessMessage = TempData[CartSuccessTempDataKey] as string;
        ViewBag.ErrorMessage = TempData[CartErrorTempDataKey] as string;

        var discountForm = new ApplyDiscountInputModel();
        var persistedDiscountCode = RestorePersistedDiscountCode();
        if (!string.IsNullOrWhiteSpace(persistedDiscountCode))
        {
            discountForm.Code = persistedDiscountCode;
        }

        ViewBag.ApplyDiscount = discountForm;
        return View("Index", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(Guid productId, int quantity = 1, Guid? offerId = null, string? returnUrl = null, CancellationToken cancellationToken = default)
    {
        if (productId == Guid.Empty)
        {
            TempData[CartErrorTempDataKey] = "محصول انتخاب شده معتبر نیست.";
            TrySetAlertForRedirect(returnUrl, "danger", "محصول انتخاب شده معتبر نیست.");
            return RedirectToLocal(returnUrl) ?? RedirectToAction(nameof(Index));
        }

        var userId = GetUserId();
        Guid? anonymousId = userId is null ? _cartCookieService.EnsureAnonymousCartId() : _cartCookieService.GetAnonymousCartId();

        // Get variantId from form
        var variantIdParam = Request.Form["variantId"].FirstOrDefault();
        Guid? variantId = null;
        if (!string.IsNullOrWhiteSpace(variantIdParam) && Guid.TryParse(variantIdParam, out var parsedVariantId))
        {
            variantId = parsedVariantId;
        }

        var result = await _mediator.Send(new AddCartItemCommand(userId, anonymousId, productId, quantity <= 0 ? 1 : quantity, offerId, variantId), cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            TempData[CartErrorTempDataKey] = result.Error ?? "امکان افزودن محصول به سبد خرید وجود ندارد.";
            TrySetAlertForRedirect(returnUrl, "danger", result.Error ?? "امکان افزودن محصول به سبد خرید وجود ندارد.");
            return RedirectToLocal(returnUrl) ?? RedirectToAction(nameof(Index));
        }

        UpdateCartCookie(result.Value.AnonymousId);
        TempData[CartSuccessTempDataKey] = "محصول به سبد خرید اضافه شد.";
        TrySetAlertForRedirect(returnUrl, "success", "محصول به سبد خرید اضافه شد.");

        return RedirectToLocal(returnUrl) ?? RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateQuantity(Guid productId, int quantity, Guid? offerId = null, CancellationToken cancellationToken = default)
    {
        var userId = GetUserId();
        var anonymousId = _cartCookieService.GetAnonymousCartId();

        // Get variantId from form
        var variantIdParam = Request.Form["variantId"].FirstOrDefault();
        Guid? variantId = null;
        if (!string.IsNullOrWhiteSpace(variantIdParam) && Guid.TryParse(variantIdParam, out var parsedVariantId))
        {
            variantId = parsedVariantId;
        }

        var result = await _mediator.Send(new UpdateCartItemQuantityCommand(userId, anonymousId, productId, quantity, variantId, offerId), cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            TempData[CartErrorTempDataKey] = result.Error ?? "امکان بروزرسانی تعداد محصول وجود ندارد.";
        }
        else
        {
            UpdateCartCookie(result.Value.AnonymousId);
            TempData[CartSuccessTempDataKey] = "تعداد محصول به‌روزرسانی شد.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateQuantityAjax(Guid productId, int quantity, Guid? offerId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("UpdateQuantityAjax called - ProductId: {ProductId}, Quantity: {Quantity}", productId, quantity);
            
            var userId = GetUserId();
            var anonymousId = _cartCookieService.GetAnonymousCartId();
            
            _logger.LogDebug("UpdateQuantityAjax - UserId: {UserId}, AnonymousId: {AnonymousId}", userId ?? "null", anonymousId);

            // Get variantId from form (like UpdateQuantity does)
            var variantIdParam = Request.Form["variantId"].FirstOrDefault();
            Guid? variantId = null;
            if (!string.IsNullOrWhiteSpace(variantIdParam) && Guid.TryParse(variantIdParam, out var parsedVariantId))
            {
                variantId = parsedVariantId;
                _logger.LogDebug("UpdateQuantityAjax - VariantId: {VariantId}", variantId);
            }

            var command = new UpdateCartItemQuantityCommand(userId, anonymousId, productId, quantity, variantId, offerId);
            _logger.LogDebug("UpdateQuantityAjax - Sending command: {@Command}", command);
            
            var result = await _mediator.Send(command, cancellationToken);
            
            _logger.LogDebug("UpdateQuantityAjax - Command result - IsSuccess: {IsSuccess}, Error: {Error}", result.IsSuccess, result.Error);
            
            if (!result.IsSuccess || result.Value is null)
            {
                _logger.LogWarning("UpdateQuantityAjax failed - ProductId: {ProductId}, Error: {Error}", productId, result.Error);
                return Json(new { success = false, error = result.Error ?? "امکان بروزرسانی تعداد محصول وجود ندارد." });
            }

            UpdateCartCookie(result.Value.AnonymousId);

            var cartDto = result.Value;
            var item = cartDto.Items.FirstOrDefault(i => i.ProductId == productId && (offerId.HasValue ? i.OfferId == offerId : true));
            if (item is null)
            {
                _logger.LogWarning("UpdateQuantityAjax - Item not found in cart - ProductId: {ProductId}", productId);
                return Json(new { success = false, error = "آیتم در سبد خرید یافت نشد." });
            }

            _logger.LogInformation("UpdateQuantityAjax succeeded - ProductId: {ProductId}, Quantity: {Quantity}", productId, item.Quantity);

            return Json(new
            {
                success = true,
                quantity = item.Quantity,
                lineTotal = item.LineTotal,
                subtotal = cartDto.Subtotal,
                discountTotal = cartDto.DiscountTotal,
                grandTotal = cartDto.GrandTotal,
                canIncreaseQuantity = item.CanIncreaseQuantity
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UpdateQuantityAjax exception - ProductId: {ProductId}, Quantity: {Quantity}", productId, quantity);
            return Json(new { success = false, error = $"خطای سرور: {ex.Message}" });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveItem(Guid productId, Guid? offerId = null, Guid? variantId = null, CancellationToken cancellationToken = default)
    {
        var userId = GetUserId();
        var anonymousId = _cartCookieService.GetAnonymousCartId();

        var result = await _mediator.Send(new RemoveCartItemCommand(userId, anonymousId, productId, variantId, offerId), cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            TempData[CartErrorTempDataKey] = result.Error ?? "امکان حذف محصول از سبد خرید وجود ندارد.";
        }
        else
        {
            UpdateCartCookie(result.Value.AnonymousId);
            TempData[CartSuccessTempDataKey] = "محصول از سبد خرید حذف شد.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApplyDiscount(ApplyDiscountInputModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            PersistDiscountCode(model.Code);
            TempData[CartErrorTempDataKey] = "کد تخفیف وارد شده معتبر نیست.";
            return RedirectToAction(nameof(Index));
        }

        var userId = GetUserId();
        var anonymousId = _cartCookieService.GetAnonymousCartId();

        var result = await _mediator.Send(new ApplyCartDiscountCommand(userId, anonymousId, model.Code!.Trim()), cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            PersistDiscountCode(model.Code);
            TempData[CartErrorTempDataKey] = result.Error ?? "امکان اعمال کد تخفیف وجود ندارد.";
        }
        else
        {
            TempData.Remove(CartDiscountCodeTempDataKey);
            UpdateCartCookie(result.Value.AnonymousId);
            TempData[CartSuccessTempDataKey] = "کد تخفیف با موفقیت اعمال شد.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ClearDiscount(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var anonymousId = _cartCookieService.GetAnonymousCartId();

        var result = await _mediator.Send(new ClearCartDiscountCommand(userId, anonymousId), cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            TempData[CartErrorTempDataKey] = result.Error ?? "امکان حذف کد تخفیف وجود ندارد.";
        }
        else
        {
            TempData.Remove(CartDiscountCodeTempDataKey);
            UpdateCartCookie(result.Value.AnonymousId);
            TempData[CartSuccessTempDataKey] = "کد تخفیف حذف شد.";
        }

        return RedirectToAction(nameof(Index));
    }

    private string? GetUserId()
        => User?.Identity?.IsAuthenticated == true
            ? User.FindFirstValue(ClaimTypes.NameIdentifier)
            : null;

    private IActionResult? RedirectToLocal(string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl) || !Url.IsLocalUrl(returnUrl))
        {
            return null;
        }

        return LocalRedirect(EncodeLocalUrl(returnUrl));
    }

    private void UpdateCartCookie(Guid? anonymousId)
    {
        if (anonymousId is null || anonymousId.Value == Guid.Empty)
        {
            return;
        }

        _cartCookieService.SetAnonymousCartId(anonymousId.Value);
    }

    private void PersistDiscountCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            TempData.Remove(CartDiscountCodeTempDataKey);
            return;
        }

        var encoded = EncodeDiscountCode(code);
        if (encoded.Length == 0)
        {
            TempData.Remove(CartDiscountCodeTempDataKey);
            return;
        }

        TempData[CartDiscountCodeTempDataKey] = encoded;
    }

    private void TrySetAlertForRedirect(string? returnUrl, string alertType, string message)
    {
        if (string.IsNullOrWhiteSpace(returnUrl) || !Url.IsLocalUrl(returnUrl) || IsCartReturnUrl(returnUrl))
        {
            return;
        }

        TempData["Alert.Type"] = alertType;
        TempData["Alert.Title"] = "سبد خرید";
        TempData["Alert.Message"] = message;
        TempData["Alert.ConfirmText"] = "باشه";
    }

    private bool IsCartReturnUrl(string? returnUrl)
    {
        var normalized = NormalizeLocalUrl(returnUrl);
        if (string.IsNullOrEmpty(normalized))
        {
            return false;
        }

        var cartUrls = new[]
        {
            NormalizeLocalUrl(Url.Action(nameof(Index))),
            NormalizeLocalUrl(Url.Action(nameof(Index), "Cart")),
            NormalizeLocalUrl("/Cart"),
            NormalizeLocalUrl("/Cart/Index")
        };

        foreach (var cartUrl in cartUrls)
        {
            if (!string.IsNullOrEmpty(cartUrl) && normalized.Equals(cartUrl, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static string NormalizeLocalUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return string.Empty;
        }

        var value = url;
        var queryIndex = value.IndexOf('?', StringComparison.Ordinal);
        if (queryIndex >= 0)
        {
            value = value[..queryIndex];
        }

        value = value.Trim();
        if (value.Length == 0)
        {
            return string.Empty;
        }

        value = value.TrimEnd('/');
        if (value.Length == 0)
        {
            value = "/";
        }

        return value.ToLowerInvariant();
    }

    private static string EncodeLocalUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return string.Empty;
        }

        if (!Uri.TryCreate(new Uri("http://localhost"), url, out var absoluteUri))
        {
            return url;
        }

        var path = absoluteUri.GetComponents(UriComponents.Path | UriComponents.KeepDelimiter, UriFormat.UriEscaped);
        var query = absoluteUri.GetComponents(UriComponents.Query | UriComponents.KeepDelimiter, UriFormat.UriEscaped);
        var fragment = absoluteUri.GetComponents(UriComponents.Fragment | UriComponents.KeepDelimiter, UriFormat.UriEscaped);

        var combined = string.Concat(path, query, fragment);
        if (!combined.StartsWith("/", StringComparison.Ordinal))
        {
            combined = "/" + combined;
        }

        return combined;
    }

    private string? RestorePersistedDiscountCode()
    {
        if (!TempData.TryGetValue(CartDiscountCodeTempDataKey, out var rawValue))
        {
            return null;
        }

        if (rawValue is not string stored || string.IsNullOrWhiteSpace(stored))
        {
            return null;
        }

        return DecodeDiscountCode(stored);
    }

    private static string EncodeDiscountCode(string code)
    {
        var trimmed = code.Trim();
        if (trimmed.Length == 0)
        {
            return string.Empty;
        }

        var bytes = Encoding.UTF8.GetBytes(trimmed);
        return WebEncoders.Base64UrlEncode(bytes);
    }

    private static string DecodeDiscountCode(string value)
    {
        try
        {
            var bytes = WebEncoders.Base64UrlDecode(value);
            return Encoding.UTF8.GetString(bytes);
        }
        catch (FormatException)
        {
            return value;
        }
        catch (ArgumentException)
        {
            return value;
        }
    }
}

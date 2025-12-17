using System;
using Microsoft.AspNetCore.Http;

namespace LogTableRenameTest.WebSite.Services.Cart;

public sealed class CartCookieService : ICartCookieService
{
    private const string CartCookieName = "LogTableRenameTest.Cart.Id";
    private static readonly TimeSpan CookieLifetime = TimeSpan.FromDays(30);

    private readonly IHttpContextAccessor _httpContextAccessor;

    public CartCookieService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? GetAnonymousCartId()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context is null)
        {
            return null;
        }

        if (!context.Request.Cookies.TryGetValue(CartCookieName, out var value) || string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return Guid.TryParse(value, out var identifier) ? identifier : null;
    }

    public Guid EnsureAnonymousCartId()
    {
        var current = GetAnonymousCartId();
        if (current is not null)
        {
            return current.Value;
        }

        var generated = Guid.NewGuid();
        SetAnonymousCartId(generated);
        return generated;
    }

    public void SetAnonymousCartId(Guid cartId)
    {
        var context = _httpContextAccessor.HttpContext;
        if (context is null || cartId == Guid.Empty)
        {
            return;
        }

        var options = new CookieOptions
        {
            HttpOnly = true,
            Secure = context.Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            IsEssential = true,
            Expires = DateTimeOffset.UtcNow.Add(CookieLifetime)
        };

        context.Response.Cookies.Append(CartCookieName, cartId.ToString(), options);
    }

    public void ClearAnonymousCartId()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context is null)
        {
            return;
        }

        if (context.Request.Cookies.ContainsKey(CartCookieName))
        {
            context.Response.Cookies.Delete(CartCookieName);
        }
    }
}

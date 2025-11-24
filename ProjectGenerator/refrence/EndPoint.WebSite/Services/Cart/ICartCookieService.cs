using System;

namespace EndPoint.WebSite.Services.Cart;

public interface ICartCookieService
{
    Guid? GetAnonymousCartId();

    Guid EnsureAnonymousCartId();

    void SetAnonymousCartId(Guid cartId);

    void ClearAnonymousCartId();
}

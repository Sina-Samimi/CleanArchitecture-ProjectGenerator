using System;
using Microsoft.AspNetCore.Http;

namespace TestAttarClone.WebSite.Services.Session;

public sealed class SessionCookieService : ISessionCookieService
{
    private const string SessionCookieName = "TestAttarClone.UserSession.Id";
    private static readonly TimeSpan CookieLifetime = TimeSpan.FromDays(30);

    private readonly IHttpContextAccessor _httpContextAccessor;

    public SessionCookieService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? GetCurrentSessionId()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context is null)
        {
            return null;
        }

        if (!context.Request.Cookies.TryGetValue(SessionCookieName, out var value) || string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return Guid.TryParse(value, out var sessionId) ? sessionId : null;
    }

    public void SetCurrentSessionId(Guid sessionId)
    {
        var context = _httpContextAccessor.HttpContext;
        if (context is null || sessionId == Guid.Empty)
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

        context.Response.Cookies.Append(SessionCookieName, sessionId.ToString(), options);
    }

    public void ClearCurrentSessionId()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context is null)
        {
            return;
        }

        context.Response.Cookies.Delete(SessionCookieName);
    }
}


using System;

namespace TestAttarClone.WebSite.Services.Session;

public interface ISessionCookieService
{
    Guid? GetCurrentSessionId();

    void SetCurrentSessionId(Guid sessionId);

    void ClearCurrentSessionId();
}


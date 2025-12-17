using System;

namespace Attar.WebSite.Services.Session;

public interface ISessionCookieService
{
    Guid? GetCurrentSessionId();

    void SetCurrentSessionId(Guid sessionId);

    void ClearCurrentSessionId();
}


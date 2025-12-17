using System;

namespace LogsDtoCloneTest.WebSite.Services.Session;

public interface ISessionCookieService
{
    Guid? GetCurrentSessionId();

    void SetCurrentSessionId(Guid sessionId);

    void ClearCurrentSessionId();
}


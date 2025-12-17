using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using LogsDtoCloneTest.Domain.Base;
using LogsDtoCloneTest.Domain.Entities.Pages;

namespace LogsDtoCloneTest.Domain.Entities.Visits;

public sealed class PageVisit : Entity
{
    public Guid? PageId { get; private set; }

    public Page? Page { get; private set; }

    public IPAddress ViewerIp { get; private set; }

    public DateOnly VisitDate { get; private set; }

    public string? UserAgent { get; private set; }

    public string? Referrer { get; private set; }

    [SetsRequiredMembers]
    private PageVisit()
    {
        ViewerIp = IPAddress.None;
    }

    [SetsRequiredMembers]
    public PageVisit(Guid? pageId, IPAddress viewerIp, DateOnly visitDate, string? userAgent = null, string? referrer = null)
    {
        PageId = pageId;
        ViewerIp = viewerIp ?? IPAddress.None;
        VisitDate = visitDate;
        UserAgent = string.IsNullOrWhiteSpace(userAgent) ? null : userAgent.Trim();
        Referrer = string.IsNullOrWhiteSpace(referrer) ? null : referrer.Trim();
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void UpdateVisitDate(DateOnly visitDate)
    {
        VisitDate = visitDate;
        UpdateDate = DateTimeOffset.UtcNow;
    }
}


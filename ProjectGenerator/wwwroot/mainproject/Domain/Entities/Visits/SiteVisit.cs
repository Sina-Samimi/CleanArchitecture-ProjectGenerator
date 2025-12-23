using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using MobiRooz.Domain.Base;

namespace MobiRooz.Domain.Entities.Visits;

public sealed class SiteVisit : Entity
{
    public IPAddress ViewerIp { get; private set; }

    public DateOnly VisitDate { get; private set; }

    public string? UserAgent { get; private set; }

    public string? Referrer { get; private set; }

    [SetsRequiredMembers]
    private SiteVisit()
    {
        ViewerIp = IPAddress.None;
    }

    [SetsRequiredMembers]
    public SiteVisit(IPAddress viewerIp, DateOnly visitDate, string? userAgent = null, string? referrer = null)
    {
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


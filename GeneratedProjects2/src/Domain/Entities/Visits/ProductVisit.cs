using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using LogsDtoCloneTest.Domain.Base;
using LogsDtoCloneTest.Domain.Entities.Catalog;

namespace LogsDtoCloneTest.Domain.Entities.Visits;

public sealed class ProductVisit : Entity
{
    public Guid? ProductId { get; private set; }

    public Product? Product { get; private set; }

    public IPAddress ViewerIp { get; private set; }

    public DateOnly VisitDate { get; private set; }

    public string? UserAgent { get; private set; }

    public string? Referrer { get; private set; }

    [SetsRequiredMembers]
    private ProductVisit()
    {
        ViewerIp = IPAddress.None;
    }

    [SetsRequiredMembers]
    public ProductVisit(Guid? productId, IPAddress viewerIp, DateOnly visitDate, string? userAgent = null, string? referrer = null)
    {
        ProductId = productId;
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


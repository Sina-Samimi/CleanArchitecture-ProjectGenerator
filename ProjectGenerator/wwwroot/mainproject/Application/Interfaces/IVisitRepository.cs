using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Attar.Application.DTOs.Visits;

namespace Attar.Application.Interfaces;

public interface IVisitRepository
{
    Task RegisterSiteVisitAsync(string viewerIp, DateOnly visitDate, string? userAgent, string? referrer, CancellationToken cancellationToken);

    Task RegisterPageVisitAsync(Guid? pageId, string viewerIp, DateOnly visitDate, string? userAgent, string? referrer, CancellationToken cancellationToken);

    Task RegisterProductVisitAsync(Guid? productId, string viewerIp, DateOnly visitDate, string? userAgent, string? referrer, CancellationToken cancellationToken);

    Task<int> GetProductVisitCountAsync(Guid productId, CancellationToken cancellationToken);

    Task<VisitStatisticsDto> GetSiteVisitStatisticsAsync(DateOnly? fromDate, DateOnly? toDate, CancellationToken cancellationToken);

    Task<VisitStatisticsDto> GetPageVisitStatisticsAsync(Guid? pageId, DateOnly? fromDate, DateOnly? toDate, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<DailyVisitDto>> GetDailySiteVisitsAsync(DateOnly? fromDate, DateOnly? toDate, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<DailyVisitDto>> GetDailyPageVisitsAsync(Guid? pageId, DateOnly? fromDate, DateOnly? toDate, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<PageVisitSummaryDto>> GetPageVisitSummariesAsync(DateOnly? fromDate, DateOnly? toDate, int pageNumber, int pageSize, CancellationToken cancellationToken);

    Task<int> GetPageVisitSummariesCountAsync(DateOnly? fromDate, DateOnly? toDate, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<DailyVisitDto>> GetDailyProductVisitsAsync(IReadOnlyCollection<Guid> productIds, DateOnly? fromDate, DateOnly? toDate, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<SiteVisitListItemDto>> GetSiteVisitsAsync(
        DateOnly? fromDate,
        DateOnly? toDate,
        string? ipAddress,
        string? deviceType,
        string? browser,
        string? operatingSystem,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken);

    Task<int> GetSiteVisitsCountAsync(
        DateOnly? fromDate,
        DateOnly? toDate,
        string? ipAddress,
        string? deviceType,
        string? browser,
        string? operatingSystem,
        CancellationToken cancellationToken);
}


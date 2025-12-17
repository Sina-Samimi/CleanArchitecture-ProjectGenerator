using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LogTableRenameTest.Application.Abstractions.Messaging;
using LogTableRenameTest.Application.DTOs.Visits;
using LogTableRenameTest.Application.Interfaces;
using LogTableRenameTest.SharedKernel.BaseTypes;

namespace LogTableRenameTest.Application.Queries.Visits;

public sealed record GetVisitStatisticsSummaryQuery(
    DateOnly? FromDate = null,
    DateOnly? ToDate = null) : IQuery<VisitStatisticsSummaryDto>;

public sealed class GetVisitStatisticsSummaryQueryHandler : IQueryHandler<GetVisitStatisticsSummaryQuery, VisitStatisticsSummaryDto>
{
    private readonly IVisitRepository _repository;

    public GetVisitStatisticsSummaryQueryHandler(IVisitRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<VisitStatisticsSummaryDto>> Handle(GetVisitStatisticsSummaryQuery request, CancellationToken cancellationToken)
    {
        // Get a sample of visits for statistics (limited to 1000 for performance)
        // This is enough to get accurate statistics
        var visits = await _repository.GetSiteVisitsAsync(
            request.FromDate,
            request.ToDate,
            null, // ipAddress
            null, // deviceType
            null, // browser
            null, // operatingSystem
            1, // pageNumber
            1000, // pageSize - limited for performance
            cancellationToken);

        if (visits.Count == 0)
        {
            return Result<VisitStatisticsSummaryDto>.Success(new VisitStatisticsSummaryDto(
                Array.Empty<DeviceTypeStatDto>(),
                Array.Empty<OperatingSystemStatDto>(),
                Array.Empty<BrowserStatDto>()));
        }

        var totalCount = visits.Count;

        // Group by Device Type - limit to top 5
        var deviceTypeStats = visits
            .GroupBy(v => v.DeviceType ?? "نامشخص")
            .Select(g => new DeviceTypeStatDto(
                g.Key,
                g.Count(),
                Math.Round((g.Count() / (double)totalCount) * 100, 2)))
            .OrderByDescending(s => s.Count)
            .Take(5)
            .ToList();

        // Group by Operating System - limit to top 6
        var osStats = visits
            .GroupBy(v => v.OperatingSystem ?? "نامشخص")
            .Select(g => new OperatingSystemStatDto(
                g.Key,
                g.Count(),
                Math.Round((g.Count() / (double)totalCount) * 100, 2)))
            .OrderByDescending(s => s.Count)
            .Take(6)
            .ToList();

        // Browser stats not needed - only 2 charts
        var browserStats = Array.Empty<BrowserStatDto>();

        var result = new VisitStatisticsSummaryDto(
            deviceTypeStats,
            osStats,
            browserStats);

        return Result<VisitStatisticsSummaryDto>.Success(result);
    }
}

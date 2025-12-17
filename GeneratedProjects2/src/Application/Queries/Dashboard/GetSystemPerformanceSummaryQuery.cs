using System;
using System.Threading;
using System.Threading.Tasks;
using LogsDtoCloneTest.Application.Abstractions.Messaging;
using LogsDtoCloneTest.Application.DTOs.Dashboard;
using LogsDtoCloneTest.Application.Interfaces;
using LogsDtoCloneTest.SharedKernel.BaseTypes;

namespace LogsDtoCloneTest.Application.Queries.Dashboard;

public sealed record GetSystemPerformanceSummaryQuery(DateTimeOffset ReferenceTime) : IQuery<SystemPerformanceSummaryDto>
{
    public sealed class Handler : IQueryHandler<GetSystemPerformanceSummaryQuery, SystemPerformanceSummaryDto>
    {
        private readonly IAdminDashboardMetricsService _metricsService;

        public Handler(IAdminDashboardMetricsService metricsService)
        {
            _metricsService = metricsService;
        }

        public async Task<Result<SystemPerformanceSummaryDto>> Handle(GetSystemPerformanceSummaryQuery request, CancellationToken cancellationToken)
        {
            var referenceTime = request.ReferenceTime == default
                ? DateTimeOffset.UtcNow
                : request.ReferenceTime;

            var summary = await _metricsService.GetSummaryAsync(referenceTime, cancellationToken);

            return Result<SystemPerformanceSummaryDto>.Success(summary);
        }
    }
}

using System;
using System.Threading;
using System.Threading.Tasks;
using LogTableRenameTest.Application.Abstractions.Messaging;
using LogTableRenameTest.Application.DTOs.Dashboard;
using LogTableRenameTest.Application.Interfaces;
using LogTableRenameTest.SharedKernel.BaseTypes;

namespace LogTableRenameTest.Application.Queries.Dashboard;

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

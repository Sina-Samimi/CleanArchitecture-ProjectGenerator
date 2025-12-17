using System;
using System.Threading;
using System.Threading.Tasks;
using LogsDtoCloneTest.Application.DTOs.Dashboard;

namespace LogsDtoCloneTest.Application.Interfaces;

public interface IAdminDashboardMetricsService
{
    Task<SystemPerformanceSummaryDto> GetSummaryAsync(DateTimeOffset referenceTime, CancellationToken cancellationToken);
}

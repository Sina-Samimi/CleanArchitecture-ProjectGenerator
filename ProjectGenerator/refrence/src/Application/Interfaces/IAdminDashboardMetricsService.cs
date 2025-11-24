using System;
using System.Threading;
using System.Threading.Tasks;
using Arsis.Application.DTOs.Dashboard;

namespace Arsis.Application.Interfaces;

public interface IAdminDashboardMetricsService
{
    Task<SystemPerformanceSummaryDto> GetSummaryAsync(DateTimeOffset referenceTime, CancellationToken cancellationToken);
}

using System;
using System.Threading;
using System.Threading.Tasks;
using MobiRooz.Application.DTOs.Dashboard;

namespace MobiRooz.Application.Interfaces;

public interface IAdminDashboardMetricsService
{
    Task<SystemPerformanceSummaryDto> GetSummaryAsync(DateTimeOffset referenceTime, CancellationToken cancellationToken);
}

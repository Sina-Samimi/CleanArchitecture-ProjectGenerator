using System;
using System.Threading;
using System.Threading.Tasks;
using TestAttarClone.Application.DTOs.Dashboard;

namespace TestAttarClone.Application.Interfaces;

public interface IAdminDashboardMetricsService
{
    Task<SystemPerformanceSummaryDto> GetSummaryAsync(DateTimeOffset referenceTime, CancellationToken cancellationToken);
}

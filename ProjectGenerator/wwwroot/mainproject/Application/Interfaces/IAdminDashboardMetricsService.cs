using System;
using System.Threading;
using System.Threading.Tasks;
using Attar.Application.DTOs.Dashboard;

namespace Attar.Application.Interfaces;

public interface IAdminDashboardMetricsService
{
    Task<SystemPerformanceSummaryDto> GetSummaryAsync(DateTimeOffset referenceTime, CancellationToken cancellationToken);
}

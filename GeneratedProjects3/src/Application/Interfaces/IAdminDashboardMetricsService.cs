using System;
using System.Threading;
using System.Threading.Tasks;
using LogTableRenameTest.Application.DTOs.Dashboard;

namespace LogTableRenameTest.Application.Interfaces;

public interface IAdminDashboardMetricsService
{
    Task<SystemPerformanceSummaryDto> GetSummaryAsync(DateTimeOffset referenceTime, CancellationToken cancellationToken);
}

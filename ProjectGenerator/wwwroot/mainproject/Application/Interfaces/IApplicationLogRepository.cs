using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Attar.Application.DTOs.Logs;

namespace Attar.Application.Interfaces;

public interface IApplicationLogRepository
{
    Task<IReadOnlyCollection<ApplicationLogListItemDto>> GetApplicationLogsAsync(
        string? level,
        DateTimeOffset? fromDate,
        DateTimeOffset? toDate,
        string? sourceContext,
        string? applicationName,
        string? machineName,
        string? environment,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken);

    Task<int> GetApplicationLogsCountAsync(
        string? level,
        DateTimeOffset? fromDate,
        DateTimeOffset? toDate,
        string? sourceContext,
        string? applicationName,
        string? machineName,
        string? environment,
        CancellationToken cancellationToken);

    Task<ApplicationLogDetailsDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken);
}


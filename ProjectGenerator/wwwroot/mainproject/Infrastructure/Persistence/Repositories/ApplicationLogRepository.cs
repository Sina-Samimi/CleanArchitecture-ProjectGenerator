using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Attar.Application.DTOs.Logs;
using Attar.Application.Interfaces;
using Attar.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Attar.Infrastructure.Persistence.Repositories;

public sealed class ApplicationLogRepository : IApplicationLogRepository
{
    private readonly LogsDbContext _logsDbContext;

    public ApplicationLogRepository(LogsDbContext logsDbContext)
    {
        _logsDbContext = logsDbContext ?? throw new ArgumentNullException(nameof(logsDbContext));
    }

    public async Task<IReadOnlyCollection<ApplicationLogListItemDto>> GetApplicationLogsAsync(
        string? level,
        DateTimeOffset? fromDate,
        DateTimeOffset? toDate,
        string? sourceContext,
        string? applicationName,
        string? machineName,
        string? environment,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = _logsDbContext.AttarApplicationLogs.AsQueryable();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(level))
        {
            query = query.Where(log => log.Level == level);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(log => log.CreateDate >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(log => log.CreateDate <= toDate.Value);
        }

        if (!string.IsNullOrWhiteSpace(sourceContext))
        {
            query = query.Where(log => log.SourceContext != null && log.SourceContext.Contains(sourceContext));
        }

        if (!string.IsNullOrWhiteSpace(applicationName))
        {
            query = query.Where(log => log.ApplicationName != null && log.ApplicationName.Contains(applicationName));
        }

        if (!string.IsNullOrWhiteSpace(machineName))
        {
            query = query.Where(log => log.MachineName != null && log.MachineName.Contains(machineName));
        }

        if (!string.IsNullOrWhiteSpace(environment))
        {
            query = query.Where(log => log.Environment != null && log.Environment.Contains(environment));
        }

        // Order by date descending (newest first)
        query = query.OrderByDescending(log => log.CreateDate);

        // Pagination
        var skip = (pageNumber - 1) * pageSize;
        query = query.Skip(skip).Take(pageSize);

        var logs = await query
            .Select(log => new ApplicationLogListItemDto(
                log.Id,
                log.Level,
                log.Message,
                log.Exception,
                log.SourceContext,
                log.RequestPath,
                log.RequestMethod,
                log.StatusCode,
                log.ElapsedMs,
                log.UserAgent,
                log.RemoteIpAddress,
                log.ApplicationName,
                log.MachineName,
                log.Environment,
                log.CreateDate))
            .ToListAsync(cancellationToken);

        return logs.AsReadOnly();
    }

    public async Task<int> GetApplicationLogsCountAsync(
        string? level,
        DateTimeOffset? fromDate,
        DateTimeOffset? toDate,
        string? sourceContext,
        string? applicationName,
        string? machineName,
        string? environment,
        CancellationToken cancellationToken)
    {
        var query = _logsDbContext.AttarApplicationLogs.AsQueryable();

        // Apply filters (same as GetApplicationLogsAsync)
        if (!string.IsNullOrWhiteSpace(level))
        {
            query = query.Where(log => log.Level == level);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(log => log.CreateDate >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(log => log.CreateDate <= toDate.Value);
        }

        if (!string.IsNullOrWhiteSpace(sourceContext))
        {
            query = query.Where(log => log.SourceContext != null && log.SourceContext.Contains(sourceContext));
        }

        if (!string.IsNullOrWhiteSpace(applicationName))
        {
            query = query.Where(log => log.ApplicationName != null && log.ApplicationName.Contains(applicationName));
        }

        if (!string.IsNullOrWhiteSpace(machineName))
        {
            query = query.Where(log => log.MachineName != null && log.MachineName.Contains(machineName));
        }

        if (!string.IsNullOrWhiteSpace(environment))
        {
            query = query.Where(log => log.Environment != null && log.Environment.Contains(environment));
        }

        return await query.CountAsync(cancellationToken);
    }

    public async Task<ApplicationLogDetailsDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var log = await _logsDbContext.AttarApplicationLogs
            .Where(l => l.Id == id)
            .Select(log => new ApplicationLogDetailsDto(
                log.Id,
                log.Level,
                log.Message,
                log.Exception,
                log.SourceContext,
                log.Properties,
                log.RequestPath,
                log.RequestMethod,
                log.StatusCode,
                log.ElapsedMs,
                log.UserAgent,
                log.RemoteIpAddress,
                log.ApplicationName,
                log.MachineName,
                log.Environment,
                log.CreateDate,
                log.UpdateDate))
            .FirstOrDefaultAsync(cancellationToken);

        return log;
    }
}


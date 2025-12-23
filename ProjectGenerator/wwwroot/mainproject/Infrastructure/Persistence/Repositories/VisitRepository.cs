using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MobiRooz.Application.DTOs.Visits;
using MobiRooz.Application.Interfaces;
using MobiRooz.Domain.Entities.Visits;
using MobiRooz.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace MobiRooz.Infrastructure.Persistence.Repositories;

public sealed class VisitRepository : IVisitRepository
{
    private readonly AppDbContext _dbContext;

    public VisitRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task RegisterSiteVisitAsync(string viewerIp, DateOnly visitDate, string? userAgent, string? referrer, CancellationToken cancellationToken)
    {
        if (!IPAddress.TryParse(viewerIp, out var ipAddress))
        {
            ipAddress = IPAddress.None;
        }

        var existingVisit = await _dbContext.SiteVisits
            .FirstOrDefaultAsync(
                visit => visit.VisitDate == visitDate && visit.ViewerIp.Equals(ipAddress),
                cancellationToken);

        if (existingVisit is null)
        {
            _dbContext.SiteVisits.Add(new SiteVisit(ipAddress, visitDate, userAgent, referrer));
        }
        else
        {
            existingVisit.UpdateVisitDate(visitDate);
        }

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            _dbContext.ChangeTracker.Clear();
        }
    }

    public async Task RegisterPageVisitAsync(Guid? pageId, string viewerIp, DateOnly visitDate, string? userAgent, string? referrer, CancellationToken cancellationToken)
    {
        if (!IPAddress.TryParse(viewerIp, out var ipAddress))
        {
            ipAddress = IPAddress.None;
        }

        var existingVisit = await _dbContext.PageVisits
            .FirstOrDefaultAsync(
                visit => visit.PageId == pageId && visit.VisitDate == visitDate && visit.ViewerIp.Equals(ipAddress),
                cancellationToken);

        if (existingVisit is null)
        {
            _dbContext.PageVisits.Add(new PageVisit(pageId, ipAddress, visitDate, userAgent, referrer));
        }
        else
        {
            existingVisit.UpdateVisitDate(visitDate);
        }

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            _dbContext.ChangeTracker.Clear();
        }
    }

    public async Task RegisterProductVisitAsync(Guid? productId, string viewerIp, DateOnly visitDate, string? userAgent, string? referrer, CancellationToken cancellationToken)
    {
        if (!IPAddress.TryParse(viewerIp, out var ipAddress))
        {
            ipAddress = IPAddress.None;
        }

        var existingVisit = await _dbContext.ProductVisits
            .FirstOrDefaultAsync(
                visit => visit.ProductId == productId && visit.VisitDate == visitDate && visit.ViewerIp.Equals(ipAddress),
                cancellationToken);

        if (existingVisit is null)
        {
            _dbContext.ProductVisits.Add(new ProductVisit(productId, ipAddress, visitDate, userAgent, referrer));
        }
        else
        {
            existingVisit.UpdateVisitDate(visitDate);
        }

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            _dbContext.ChangeTracker.Clear();
        }
    }

    public async Task<int> GetProductVisitCountAsync(Guid productId, CancellationToken cancellationToken)
    {
        return await _dbContext.ProductVisits
            .AsNoTracking()
            .Where(visit => visit.ProductId == productId && !visit.IsDeleted)
            .CountAsync(cancellationToken);
    }

    public async Task<VisitStatisticsDto> GetSiteVisitStatisticsAsync(DateOnly? fromDate, DateOnly? toDate, CancellationToken cancellationToken)
    {
        var query = _dbContext.SiteVisits.AsNoTracking();

        if (fromDate.HasValue)
        {
            query = query.Where(visit => visit.VisitDate >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(visit => visit.VisitDate <= toDate.Value);
        }

        var visits = await query.ToListAsync(cancellationToken);

        if (visits.Count == 0)
        {
            return new VisitStatisticsDto(0, 0, 0, null, null);
        }

        var totalVisits = visits.Count;
        var uniqueVisitors = visits.Select(v => v.ViewerIp.ToString()).Distinct().Count();
        var firstVisit = visits.OrderBy(v => v.VisitDate).ThenBy(v => v.CreateDate).First();
        var lastVisit = visits.OrderByDescending(v => v.VisitDate).ThenByDescending(v => v.CreateDate).First();

        return new VisitStatisticsDto(
            totalVisits,
            uniqueVisitors,
            0,
            firstVisit.VisitDate.ToDateTime(TimeOnly.MinValue),
            lastVisit.VisitDate.ToDateTime(TimeOnly.MinValue));
    }

    public async Task<VisitStatisticsDto> GetPageVisitStatisticsAsync(Guid? pageId, DateOnly? fromDate, DateOnly? toDate, CancellationToken cancellationToken)
    {
        var query = _dbContext.PageVisits.AsNoTracking();

        if (pageId.HasValue)
        {
            query = query.Where(visit => visit.PageId == pageId.Value);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(visit => visit.VisitDate >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(visit => visit.VisitDate <= toDate.Value);
        }

        var visits = await query.ToListAsync(cancellationToken);

        if (visits.Count == 0)
        {
            return new VisitStatisticsDto(0, 0, 0, null, null);
        }

        var totalPageVisits = visits.Count;
        var uniqueVisitors = visits.Select(v => v.ViewerIp.ToString()).Distinct().Count();
        var firstVisit = visits.OrderBy(v => v.VisitDate).ThenBy(v => v.CreateDate).First();
        var lastVisit = visits.OrderByDescending(v => v.VisitDate).ThenByDescending(v => v.CreateDate).First();

        return new VisitStatisticsDto(
            0,
            uniqueVisitors,
            totalPageVisits,
            firstVisit.VisitDate.ToDateTime(TimeOnly.MinValue),
            lastVisit.VisitDate.ToDateTime(TimeOnly.MinValue));
    }

    public async Task<IReadOnlyCollection<DailyVisitDto>> GetDailySiteVisitsAsync(DateOnly? fromDate, DateOnly? toDate, CancellationToken cancellationToken)
    {
        var query = _dbContext.SiteVisits.AsNoTracking();

        if (fromDate.HasValue)
        {
            query = query.Where(visit => visit.VisitDate >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(visit => visit.VisitDate <= toDate.Value);
        }

        var visits = await query.ToListAsync(cancellationToken);

        var dailyVisits = visits
            .GroupBy(visit => visit.VisitDate)
            .Select(g => new DailyVisitDto(
                g.Key,
                g.Count(),
                g.Select(v => v.ViewerIp.ToString()).Distinct().Count()))
            .OrderBy(d => d.Date)
            .ToList();

        return dailyVisits;
    }

    public async Task<IReadOnlyCollection<DailyVisitDto>> GetDailyPageVisitsAsync(Guid? pageId, DateOnly? fromDate, DateOnly? toDate, CancellationToken cancellationToken)
    {
        var query = _dbContext.PageVisits.AsNoTracking();

        if (pageId.HasValue)
        {
            query = query.Where(visit => visit.PageId == pageId.Value);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(visit => visit.VisitDate >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(visit => visit.VisitDate <= toDate.Value);
        }

        var visits = await query.ToListAsync(cancellationToken);

        var dailyVisits = visits
            .GroupBy(visit => visit.VisitDate)
            .Select(g => new DailyVisitDto(
                g.Key,
                g.Count(),
                g.Select(v => v.ViewerIp.ToString()).Distinct().Count()))
            .OrderBy(d => d.Date)
            .ToList();

        return dailyVisits;
    }

    public async Task<IReadOnlyCollection<DailyVisitDto>> GetDailyProductVisitsAsync(IReadOnlyCollection<Guid> productIds, DateOnly? fromDate, DateOnly? toDate, CancellationToken cancellationToken)
    {
        var query = _dbContext.ProductVisits.AsNoTracking()
            .Where(visit => !visit.IsDeleted && productIds.Contains(visit.ProductId!.Value));

        if (fromDate.HasValue)
        {
            query = query.Where(visit => visit.VisitDate >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(visit => visit.VisitDate <= toDate.Value);
        }

        var visits = await query.ToListAsync(cancellationToken);

        var dailyVisits = visits
            .GroupBy(visit => visit.VisitDate)
            .Select(g => new DailyVisitDto(
                g.Key,
                g.Count(),
                g.Select(v => v.ViewerIp.ToString()).Distinct().Count()))
            .OrderBy(d => d.Date)
            .ToList();

        return dailyVisits;
    }

    public async Task<IReadOnlyCollection<PageVisitSummaryDto>> GetPageVisitSummariesAsync(DateOnly? fromDate, DateOnly? toDate, int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var query = _dbContext.PageVisits
            .AsNoTracking()
            .Include(visit => visit.Page)
            .AsQueryable();

        if (fromDate.HasValue)
        {
            query = query.Where(visit => visit.VisitDate >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(visit => visit.VisitDate <= toDate.Value);
        }

        var visits = await query
            .Where(visit => visit.Page != null)
            .ToListAsync(cancellationToken);

        var summaries = visits
            .GroupBy(visit => new { visit.PageId, visit.Page!.Title, visit.Page.Slug })
            .Select(g => new PageVisitSummaryDto(
                g.Key.PageId,
                g.Key.Title,
                g.Key.Slug,
                g.Count(),
                g.Select(v => v.ViewerIp.ToString()).Distinct().Count(),
                g.Max(v => (DateOnly?)v.VisitDate)))
            .OrderByDescending(s => s.TotalVisits)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return summaries;
    }

    public async Task<int> GetPageVisitSummariesCountAsync(DateOnly? fromDate, DateOnly? toDate, CancellationToken cancellationToken)
    {
        var query = _dbContext.PageVisits
            .AsNoTracking()
            .Include(visit => visit.Page)
            .AsQueryable();

        if (fromDate.HasValue)
        {
            query = query.Where(visit => visit.VisitDate >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(visit => visit.VisitDate <= toDate.Value);
        }

        var visits = await query
            .Where(visit => visit.Page != null)
            .ToListAsync(cancellationToken);

        var count = visits
            .GroupBy(visit => new { visit.PageId, visit.Page!.Title, visit.Page.Slug })
            .Count();

        return count;
    }

    public async Task<IReadOnlyCollection<SiteVisitListItemDto>> GetSiteVisitsAsync(
        DateOnly? fromDate,
        DateOnly? toDate,
        string? ipAddress,
        string? deviceType,
        string? browser,
        string? operatingSystem,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.SiteVisits.AsNoTracking().AsQueryable();

        if (fromDate.HasValue)
        {
            query = query.Where(visit => visit.VisitDate >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(visit => visit.VisitDate <= toDate.Value);
        }

        if (!string.IsNullOrWhiteSpace(ipAddress))
        {
            if (IPAddress.TryParse(ipAddress, out var ip))
            {
                query = query.Where(visit => visit.ViewerIp.Equals(ip));
            }
            else
            {
                // If IP is not valid, try to match as string
                query = query.Where(visit => visit.ViewerIp.ToString().Contains(ipAddress));
            }
        }

        // Get all matching visits (before User Agent filtering)
        var allVisits = await query
            .OrderByDescending(visit => visit.VisitDate)
            .ThenByDescending(visit => visit.UpdateDate)
            .ToListAsync(cancellationToken);

        // Parse User Agent and filter
        var parsedVisits = allVisits
            .Select(visit =>
            {
                var parsed = UserAgentParser.Parse(visit.UserAgent);
                return new
                {
                    Visit = visit,
                    Parsed = parsed,
                    Dto = new SiteVisitListItemDto(
                        visit.Id,
                        visit.ViewerIp.ToString(),
                        visit.VisitDate,
                        visit.UserAgent,
                        visit.Referrer,
                        visit.UpdateDate,
                        parsed.DeviceType,
                        parsed.OperatingSystem,
                        parsed.OsVersion,
                        parsed.Browser,
                        parsed.BrowserVersion,
                        parsed.Engine)
                };
            })
            .Where(x =>
                (string.IsNullOrWhiteSpace(deviceType) || 
                 string.Equals(x.Parsed.DeviceType, deviceType, StringComparison.OrdinalIgnoreCase)) &&
                (string.IsNullOrWhiteSpace(browser) || 
                 (x.Parsed.Browser?.Contains(browser, StringComparison.OrdinalIgnoreCase) ?? false)) &&
                (string.IsNullOrWhiteSpace(operatingSystem) || 
                 (x.Parsed.OperatingSystem?.Contains(operatingSystem, StringComparison.OrdinalIgnoreCase) ?? false)))
            .ToList();

        // Apply pagination after filtering
        var result = parsedVisits
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(x => x.Dto)
            .ToList();

        return result;
    }

    public async Task<int> GetSiteVisitsCountAsync(
        DateOnly? fromDate,
        DateOnly? toDate,
        string? ipAddress,
        string? deviceType,
        string? browser,
        string? operatingSystem,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.SiteVisits.AsNoTracking().AsQueryable();

        if (fromDate.HasValue)
        {
            query = query.Where(visit => visit.VisitDate >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(visit => visit.VisitDate <= toDate.Value);
        }

        if (!string.IsNullOrWhiteSpace(ipAddress))
        {
            if (IPAddress.TryParse(ipAddress, out var ip))
            {
                query = query.Where(visit => visit.ViewerIp.Equals(ip));
            }
            else
            {
                // If IP is not valid, try to match as string
                query = query.Where(visit => visit.ViewerIp.ToString().Contains(ipAddress));
            }
        }

        var visits = await query.ToListAsync(cancellationToken);

        // Parse and filter by User Agent properties
        var filtered = visits
            .Select(visit =>
            {
                var parsed = UserAgentParser.Parse(visit.UserAgent);
                return new
                {
                    Visit = visit,
                    Parsed = parsed
                };
            })
            .Where(x =>
                (string.IsNullOrWhiteSpace(deviceType) || 
                 string.Equals(x.Parsed.DeviceType, deviceType, StringComparison.OrdinalIgnoreCase)) &&
                (string.IsNullOrWhiteSpace(browser) || 
                 (x.Parsed.Browser?.Contains(browser, StringComparison.OrdinalIgnoreCase) ?? false)) &&
                (string.IsNullOrWhiteSpace(operatingSystem) || 
                 (x.Parsed.OperatingSystem?.Contains(operatingSystem, StringComparison.OrdinalIgnoreCase) ?? false)))
            .Count();

        return filtered;
    }
}


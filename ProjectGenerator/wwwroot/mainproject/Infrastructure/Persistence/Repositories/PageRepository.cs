using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Attar.Application.DTOs.Pages;
using Attar.Application.Interfaces;
using Attar.Domain.Entities.Pages;
using Microsoft.EntityFrameworkCore;

namespace Attar.Infrastructure.Persistence.Repositories;

public sealed class PageRepository : IPageRepository
{
    private readonly AppDbContext _dbContext;

    public PageRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Page?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => await _dbContext.Pages
            .AsNoTracking()
            .FirstOrDefaultAsync(page => page.Id == id, cancellationToken);

    public async Task<Page?> GetBySlugAsync(string slug, CancellationToken cancellationToken)
        => await _dbContext.Pages
            .AsNoTracking()
            .FirstOrDefaultAsync(page => page.Slug == slug, cancellationToken);

    public async Task<Page?> GetBySlugForUpdateAsync(string slug, CancellationToken cancellationToken)
        => await _dbContext.Pages
            .FirstOrDefaultAsync(page => page.Slug == slug, cancellationToken);

    public async Task<IReadOnlyCollection<Page>> GetAllAsync(CancellationToken cancellationToken)
        => await _dbContext.Pages
            .AsNoTracking()
            .OrderByDescending(page => page.CreateDate)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyCollection<Page>> GetPublishedAsync(CancellationToken cancellationToken)
        => await _dbContext.Pages
            .AsNoTracking()
            .Where(page => page.IsPublished)
            .OrderByDescending(page => page.PublishedAt ?? page.CreateDate)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyCollection<Page>> GetFooterPagesAsync(CancellationToken cancellationToken)
        => await _dbContext.Pages
            .AsNoTracking()
            .Where(page => page.IsPublished && page.ShowInFooter)
            .OrderBy(page => page.Title)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyCollection<Page>> GetQuickAccessPagesAsync(CancellationToken cancellationToken)
        => await _dbContext.Pages
            .AsNoTracking()
            .Where(page => page.IsPublished && page.ShowInQuickAccess)
            .OrderBy(page => page.Title)
            .ToListAsync(cancellationToken);

    public async Task<bool> SlugExistsAsync(string slug, Guid? excludeId, CancellationToken cancellationToken)
    {
        var query = _dbContext.Pages.Where(page => page.Slug == slug);

        if (excludeId.HasValue)
        {
            query = query.Where(page => page.Id != excludeId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    public async Task AddAsync(Page page, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(page);
        await _dbContext.Pages.AddAsync(page, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Page page, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(page);
        _dbContext.Pages.Update(page);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Page page, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(page);
        _dbContext.Pages.Remove(page);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<PageStatisticsDto> GetStatisticsAsync(CancellationToken cancellationToken)
    {
        var allPages = await _dbContext.Pages
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var totalPages = allPages.Count;
        var publishedPages = allPages.Count(p => p.IsPublished);
        var draftPages = totalPages - publishedPages;
        var totalViews = allPages.Sum(p => p.ViewCount);
        var footerPages = allPages.Count(p => p.IsPublished && p.ShowInFooter);
        var quickAccessPages = allPages.Count(p => p.IsPublished && p.ShowInQuickAccess);

        return new PageStatisticsDto(
            totalPages,
            publishedPages,
            draftPages,
            totalViews,
            footerPages,
            quickAccessPages);
    }
}


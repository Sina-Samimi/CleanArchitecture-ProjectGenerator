using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LogTableRenameTest.Application.Interfaces;
using LogTableRenameTest.Domain.Entities.Settings;
using Microsoft.EntityFrameworkCore;

namespace LogTableRenameTest.Infrastructure.Persistence.Repositories;

public sealed class BannerRepository : IBannerRepository
{
    private readonly AppDbContext _dbContext;

    public BannerRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Banner?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _dbContext.Banners
            .AsNoTracking()
            .Where(b => b.Id == id && !b.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Banner?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _dbContext.Banners
            .Where(b => b.Id == id && !b.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<Banner>> GetAllAsync(
        bool? isActive,
        bool? showOnHomePage,
        bool? isSlider,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.Banners
            .AsNoTracking()
            .Where(b => !b.IsDeleted);

        if (isActive.HasValue)
        {
            query = query.Where(b => b.IsActive == isActive.Value);
        }

        if (showOnHomePage.HasValue)
        {
            query = query.Where(b => b.ShowOnHomePage == showOnHomePage.Value);
        }

        // Note: isSlider parameter is kept for backward compatibility but ignored
        // Slider mode is now controlled by SiteSettings.BannersAsSlider

        query = query.OrderBy(b => b.DisplayOrder)
            .ThenByDescending(b => b.CreateDate);

        var skip = (pageNumber - 1) * pageSize;
        var banners = await query
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return banners.AsReadOnly();
    }

    public async Task<int> GetCountAsync(
        bool? isActive,
        bool? showOnHomePage,
        bool? isSlider,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.Banners
            .AsNoTracking()
            .Where(b => !b.IsDeleted);

        if (isActive.HasValue)
        {
            query = query.Where(b => b.IsActive == isActive.Value);
        }

        if (showOnHomePage.HasValue)
        {
            query = query.Where(b => b.ShowOnHomePage == showOnHomePage.Value);
        }

        // Note: isSlider parameter is kept for backward compatibility but ignored
        // Slider mode is now controlled by SiteSettings.BannersAsSlider

        return await query.CountAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<Banner>> GetActiveBannersForHomePageAsync(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;

        var banners = await _dbContext.Banners
            .AsNoTracking()
            .Where(b => !b.IsDeleted &&
                       b.IsActive &&
                       b.ShowOnHomePage &&
                       (!b.StartDate.HasValue || b.StartDate.Value <= now) &&
                       (!b.EndDate.HasValue || b.EndDate.Value >= now))
            .OrderBy(b => b.DisplayOrder)
            .ThenByDescending(b => b.CreateDate)
            .ToListAsync(cancellationToken);

        return banners.AsReadOnly();
    }

    public async Task AddAsync(Banner banner, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(banner);

        await _dbContext.Banners.AddAsync(banner, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Banner banner, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(banner);

        _dbContext.Banners.Update(banner);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Banner banner, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(banner);

        banner.IsDeleted = true;
        banner.UpdateDate = DateTimeOffset.UtcNow;
        _dbContext.Banners.Update(banner);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}


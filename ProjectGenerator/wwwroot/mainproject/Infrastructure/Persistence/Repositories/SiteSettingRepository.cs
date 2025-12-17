using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Attar.Application.Interfaces;
using Attar.Domain.Entities.Settings;
using Microsoft.EntityFrameworkCore;

namespace Attar.Infrastructure.Persistence.Repositories;

public sealed class SiteSettingRepository : ISiteSettingRepository
{
    private readonly AppDbContext _dbContext;

    public SiteSettingRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<SiteSetting?> GetCurrentAsync(CancellationToken cancellationToken)
        => await _dbContext.SiteSettings
            .AsNoTracking()
            .OrderByDescending(setting => setting.UpdateDate)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<SiteSetting?> GetCurrentForUpdateAsync(CancellationToken cancellationToken)
        => await _dbContext.SiteSettings
            .OrderByDescending(setting => setting.UpdateDate)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task AddAsync(SiteSetting setting, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(setting);

        await _dbContext.SiteSettings.AddAsync(setting, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(SiteSetting setting, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(setting);

        _dbContext.SiteSettings.Update(setting);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}

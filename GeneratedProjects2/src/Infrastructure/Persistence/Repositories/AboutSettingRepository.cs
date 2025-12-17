using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LogsDtoCloneTest.Application.Interfaces;
using LogsDtoCloneTest.Domain.Entities.Settings;
using Microsoft.EntityFrameworkCore;

namespace LogsDtoCloneTest.Infrastructure.Persistence.Repositories;

public sealed class AboutSettingRepository : IAboutSettingRepository
{
    private readonly AppDbContext _dbContext;

    public AboutSettingRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<AboutSetting?> GetCurrentAsync(CancellationToken cancellationToken)
        => await _dbContext.AboutSettings
            .AsNoTracking()
            .OrderByDescending(setting => setting.UpdateDate)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<AboutSetting?> GetCurrentForUpdateAsync(CancellationToken cancellationToken)
        => await _dbContext.AboutSettings
            .OrderByDescending(setting => setting.UpdateDate)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task AddAsync(AboutSetting setting, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(setting);

        await _dbContext.AboutSettings.AddAsync(setting, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(AboutSetting setting, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(setting);

        _dbContext.AboutSettings.Update(setting);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}


using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MobiRooz.Application.Interfaces;
using MobiRooz.Domain.Entities.Settings;
using Microsoft.EntityFrameworkCore;

namespace MobiRooz.Infrastructure.Persistence.Repositories;

public sealed class SmsSettingRepository : ISmsSettingRepository
{
    private readonly AppDbContext _dbContext;

    public SmsSettingRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<SmsSetting?> GetCurrentAsync(CancellationToken cancellationToken)
        => await _dbContext.SmsSettings
            .AsNoTracking()
            .OrderByDescending(setting => setting.UpdateDate)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<SmsSetting?> GetCurrentForUpdateAsync(CancellationToken cancellationToken)
        => await _dbContext.SmsSettings
            .OrderByDescending(setting => setting.UpdateDate)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task AddAsync(SmsSetting setting, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(setting);

        await _dbContext.SmsSettings.AddAsync(setting, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(SmsSetting setting, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(setting);

        _dbContext.SmsSettings.Update(setting);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Attar.Application.Interfaces;
using Attar.Domain.Entities.Settings;
using Microsoft.EntityFrameworkCore;

namespace Attar.Infrastructure.Persistence.Repositories;

public sealed class FinancialSettingRepository : IFinancialSettingRepository
{
    private readonly AppDbContext _dbContext;

    public FinancialSettingRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<FinancialSetting?> GetCurrentAsync(CancellationToken cancellationToken)
        => await _dbContext.FinancialSettings
            .AsNoTracking()
            .OrderByDescending(setting => setting.UpdateDate)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<FinancialSetting?> GetCurrentForUpdateAsync(CancellationToken cancellationToken)
        => await _dbContext.FinancialSettings
            .OrderByDescending(setting => setting.UpdateDate)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task AddAsync(FinancialSetting setting, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(setting);

        await _dbContext.FinancialSettings.AddAsync(setting, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(FinancialSetting setting, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(setting);

        _dbContext.FinancialSettings.Update(setting);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}

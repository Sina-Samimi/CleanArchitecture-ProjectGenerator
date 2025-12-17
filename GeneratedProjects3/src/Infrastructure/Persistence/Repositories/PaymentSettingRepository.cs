using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LogTableRenameTest.Application.Interfaces;
using LogTableRenameTest.Domain.Entities.Settings;
using Microsoft.EntityFrameworkCore;

namespace LogTableRenameTest.Infrastructure.Persistence.Repositories;

public sealed class PaymentSettingRepository : IPaymentSettingRepository
{
    private readonly AppDbContext _dbContext;

    public PaymentSettingRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PaymentSetting?> GetCurrentAsync(CancellationToken cancellationToken)
        => await _dbContext.PaymentSettings
            .AsNoTracking()
            .OrderByDescending(setting => setting.UpdateDate)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<PaymentSetting?> GetCurrentForUpdateAsync(CancellationToken cancellationToken)
        => await _dbContext.PaymentSettings
            .OrderByDescending(setting => setting.UpdateDate)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task AddAsync(PaymentSetting setting, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(setting);

        await _dbContext.PaymentSettings.AddAsync(setting, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(PaymentSetting setting, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(setting);

        _dbContext.PaymentSettings.Update(setting);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}

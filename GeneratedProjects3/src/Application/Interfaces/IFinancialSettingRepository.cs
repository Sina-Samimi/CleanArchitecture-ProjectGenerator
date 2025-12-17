using System.Threading;
using System.Threading.Tasks;
using LogTableRenameTest.Domain.Entities.Settings;

namespace LogTableRenameTest.Application.Interfaces;

public interface IFinancialSettingRepository
{
    Task<FinancialSetting?> GetCurrentAsync(CancellationToken cancellationToken);

    Task<FinancialSetting?> GetCurrentForUpdateAsync(CancellationToken cancellationToken);

    Task AddAsync(FinancialSetting setting, CancellationToken cancellationToken);

    Task UpdateAsync(FinancialSetting setting, CancellationToken cancellationToken);
}

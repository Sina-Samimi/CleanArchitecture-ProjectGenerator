using System.Threading;
using System.Threading.Tasks;
using TestAttarClone.Domain.Entities.Settings;

namespace TestAttarClone.Application.Interfaces;

public interface IFinancialSettingRepository
{
    Task<FinancialSetting?> GetCurrentAsync(CancellationToken cancellationToken);

    Task<FinancialSetting?> GetCurrentForUpdateAsync(CancellationToken cancellationToken);

    Task AddAsync(FinancialSetting setting, CancellationToken cancellationToken);

    Task UpdateAsync(FinancialSetting setting, CancellationToken cancellationToken);
}

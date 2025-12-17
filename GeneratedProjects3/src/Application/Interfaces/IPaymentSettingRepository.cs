using System.Threading;
using System.Threading.Tasks;
using LogTableRenameTest.Domain.Entities.Settings;

namespace LogTableRenameTest.Application.Interfaces;

public interface IPaymentSettingRepository
{
    Task<PaymentSetting?> GetCurrentAsync(CancellationToken cancellationToken);

    Task<PaymentSetting?> GetCurrentForUpdateAsync(CancellationToken cancellationToken);

    Task AddAsync(PaymentSetting setting, CancellationToken cancellationToken);

    Task UpdateAsync(PaymentSetting setting, CancellationToken cancellationToken);
}

using System.Threading;
using System.Threading.Tasks;
using LogTableRenameTest.Domain.Entities.Settings;

namespace LogTableRenameTest.Application.Interfaces;

public interface ISmsSettingRepository
{
    Task<SmsSetting?> GetCurrentAsync(CancellationToken cancellationToken);

    Task<SmsSetting?> GetCurrentForUpdateAsync(CancellationToken cancellationToken);

    Task AddAsync(SmsSetting setting, CancellationToken cancellationToken);

    Task UpdateAsync(SmsSetting setting, CancellationToken cancellationToken);
}

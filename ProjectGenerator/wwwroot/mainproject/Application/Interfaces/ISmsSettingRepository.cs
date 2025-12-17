using System.Threading;
using System.Threading.Tasks;
using Attar.Domain.Entities.Settings;

namespace Attar.Application.Interfaces;

public interface ISmsSettingRepository
{
    Task<SmsSetting?> GetCurrentAsync(CancellationToken cancellationToken);

    Task<SmsSetting?> GetCurrentForUpdateAsync(CancellationToken cancellationToken);

    Task AddAsync(SmsSetting setting, CancellationToken cancellationToken);

    Task UpdateAsync(SmsSetting setting, CancellationToken cancellationToken);
}

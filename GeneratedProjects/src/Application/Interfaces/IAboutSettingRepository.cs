using System.Threading;
using System.Threading.Tasks;
using TestAttarClone.Domain.Entities.Settings;

namespace TestAttarClone.Application.Interfaces;

public interface IAboutSettingRepository
{
    Task<AboutSetting?> GetCurrentAsync(CancellationToken cancellationToken);

    Task<AboutSetting?> GetCurrentForUpdateAsync(CancellationToken cancellationToken);

    Task AddAsync(AboutSetting setting, CancellationToken cancellationToken);

    Task UpdateAsync(AboutSetting setting, CancellationToken cancellationToken);
}


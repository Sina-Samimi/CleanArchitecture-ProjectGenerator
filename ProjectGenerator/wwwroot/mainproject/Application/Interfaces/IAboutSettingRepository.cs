using System.Threading;
using System.Threading.Tasks;
using Attar.Domain.Entities.Settings;

namespace Attar.Application.Interfaces;

public interface IAboutSettingRepository
{
    Task<AboutSetting?> GetCurrentAsync(CancellationToken cancellationToken);

    Task<AboutSetting?> GetCurrentForUpdateAsync(CancellationToken cancellationToken);

    Task AddAsync(AboutSetting setting, CancellationToken cancellationToken);

    Task UpdateAsync(AboutSetting setting, CancellationToken cancellationToken);
}


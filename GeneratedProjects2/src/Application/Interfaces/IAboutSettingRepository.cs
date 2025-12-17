using System.Threading;
using System.Threading.Tasks;
using LogsDtoCloneTest.Domain.Entities.Settings;

namespace LogsDtoCloneTest.Application.Interfaces;

public interface IAboutSettingRepository
{
    Task<AboutSetting?> GetCurrentAsync(CancellationToken cancellationToken);

    Task<AboutSetting?> GetCurrentForUpdateAsync(CancellationToken cancellationToken);

    Task AddAsync(AboutSetting setting, CancellationToken cancellationToken);

    Task UpdateAsync(AboutSetting setting, CancellationToken cancellationToken);
}


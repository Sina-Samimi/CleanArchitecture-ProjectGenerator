using System.Threading;
using System.Threading.Tasks;
using Attar.Domain.Entities.Settings;

namespace Attar.Application.Interfaces;

public interface ISiteSettingRepository
{
    Task<SiteSetting?> GetCurrentAsync(CancellationToken cancellationToken);

    Task<SiteSetting?> GetCurrentForUpdateAsync(CancellationToken cancellationToken);

    Task AddAsync(SiteSetting setting, CancellationToken cancellationToken);

    Task UpdateAsync(SiteSetting setting, CancellationToken cancellationToken);
}

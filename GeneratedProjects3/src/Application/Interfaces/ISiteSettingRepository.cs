using System.Threading;
using System.Threading.Tasks;
using LogTableRenameTest.Domain.Entities.Settings;

namespace LogTableRenameTest.Application.Interfaces;

public interface ISiteSettingRepository
{
    Task<SiteSetting?> GetCurrentAsync(CancellationToken cancellationToken);

    Task<SiteSetting?> GetCurrentForUpdateAsync(CancellationToken cancellationToken);

    Task AddAsync(SiteSetting setting, CancellationToken cancellationToken);

    Task UpdateAsync(SiteSetting setting, CancellationToken cancellationToken);
}

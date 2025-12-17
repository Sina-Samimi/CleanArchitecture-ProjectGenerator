using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Attar.Domain.Entities.Settings;

namespace Attar.Application.Interfaces;

public interface IBannerRepository
{
    Task<Banner?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<Banner?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<Banner>> GetAllAsync(
        bool? isActive,
        bool? showOnHomePage,
        bool? isSlider,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken);

    Task<int> GetCountAsync(
        bool? isActive,
        bool? showOnHomePage,
        bool? isSlider,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<Banner>> GetActiveBannersForHomePageAsync(CancellationToken cancellationToken);

    Task AddAsync(Banner banner, CancellationToken cancellationToken);

    Task UpdateAsync(Banner banner, CancellationToken cancellationToken);

    Task DeleteAsync(Banner banner, CancellationToken cancellationToken);
}


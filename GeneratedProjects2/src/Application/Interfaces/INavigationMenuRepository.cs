using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LogsDtoCloneTest.Domain.Entities.Navigation;

namespace LogsDtoCloneTest.Application.Interfaces;

public interface INavigationMenuRepository
{
    Task<IReadOnlyList<NavigationMenuItem>> GetAllAsync(CancellationToken cancellationToken);

    Task<NavigationMenuItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task AddAsync(NavigationMenuItem item, CancellationToken cancellationToken);

    Task UpdateAsync(NavigationMenuItem item, CancellationToken cancellationToken);

    Task RemoveAsync(NavigationMenuItem item, CancellationToken cancellationToken);

    Task<bool> HasChildrenAsync(Guid id, CancellationToken cancellationToken);
}

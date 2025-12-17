using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TestAttarClone.Application.Interfaces;
using TestAttarClone.Domain.Entities.Navigation;
using Microsoft.EntityFrameworkCore;

namespace TestAttarClone.Infrastructure.Persistence.Repositories;

public sealed class NavigationMenuRepository : INavigationMenuRepository
{
    private readonly AppDbContext _dbContext;

    public NavigationMenuRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<NavigationMenuItem>> GetAllAsync(CancellationToken cancellationToken)
        => await _dbContext.NavigationMenuItems
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    public async Task<NavigationMenuItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => await _dbContext.NavigationMenuItems
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

    public async Task AddAsync(NavigationMenuItem item, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(item);

        await _dbContext.NavigationMenuItems.AddAsync(item, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(NavigationMenuItem item, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(item);

        _dbContext.NavigationMenuItems.Update(item);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveAsync(NavigationMenuItem item, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(item);

        _dbContext.NavigationMenuItems.Remove(item);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> HasChildrenAsync(Guid id, CancellationToken cancellationToken)
        => await _dbContext.NavigationMenuItems
            .AnyAsync(item => item.ParentId == id, cancellationToken);
}

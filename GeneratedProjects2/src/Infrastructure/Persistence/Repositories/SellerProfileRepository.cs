using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LogsDtoCloneTest.Application.Interfaces;
using LogsDtoCloneTest.Domain.Entities.Sellers;
using LogsDtoCloneTest.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LogsDtoCloneTest.Infrastructure.Persistence.Repositories;

public sealed class SellerProfileRepository : ISellerProfileRepository
{
    private readonly AppDbContext _dbContext;

    public SellerProfileRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<SellerProfile>> GetAllAsync(CancellationToken cancellationToken)
        => await _dbContext.SellerProfiles
            .AsNoTracking()
            .Where(seller => !seller.IsDeleted)
            .OrderByDescending(seller => seller.UpdateDate)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyCollection<SellerProfile>> GetActiveAsync(CancellationToken cancellationToken)
        => await _dbContext.SellerProfiles
            .AsNoTracking()
            .Where(seller => !seller.IsDeleted && seller.IsActive)
            .OrderBy(seller => seller.DisplayName)
            .ToListAsync(cancellationToken);

    public async Task<SellerProfile?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => await _dbContext.SellerProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(seller => seller.Id == id && !seller.IsDeleted, cancellationToken);

    public async Task<SellerProfile?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken)
        => await _dbContext.SellerProfiles
            .FirstOrDefaultAsync(seller => seller.Id == id && !seller.IsDeleted, cancellationToken);

    public async Task<SellerProfile?> GetByUserIdAsync(string userId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return null;
        }

        var normalized = userId.Trim();

        return await _dbContext.SellerProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(
                seller => !seller.IsDeleted && seller.UserId == normalized,
                cancellationToken);
    }

    public async Task<bool> ExistsByUserIdAsync(string userId, Guid? excludeId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return false;
        }

        var normalized = userId.Trim();

        var query = _dbContext.SellerProfiles
            .AsNoTracking()
            .Where(seller => !seller.IsDeleted && seller.UserId == normalized);

        if (excludeId.HasValue)
        {
            var excluded = excludeId.Value;
            query = query.Where(seller => seller.Id != excluded);
        }

        return await query.AnyAsync(cancellationToken);
    }

    public async Task AddAsync(SellerProfile seller, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(seller);

        await _dbContext.SellerProfiles.AddAsync(seller, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(SellerProfile seller, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(seller);

        _dbContext.SellerProfiles.Update(seller);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}

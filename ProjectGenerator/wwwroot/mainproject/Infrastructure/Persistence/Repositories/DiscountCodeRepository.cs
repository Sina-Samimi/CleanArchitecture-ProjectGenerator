using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MobiRooz.Application.Interfaces;
using MobiRooz.Domain.Entities.Discounts;
using Microsoft.EntityFrameworkCore;

namespace MobiRooz.Infrastructure.Persistence.Repositories;

public sealed class DiscountCodeRepository : IDiscountCodeRepository
{
    private readonly AppDbContext _dbContext;

    public DiscountCodeRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<DiscountCode?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => _dbContext.DiscountCodes
            .AsTracking()
            .FirstOrDefaultAsync(discount => discount.Id == id, cancellationToken);

    public Task<DiscountCode?> GetByCodeAsync(string code, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(code))
        {
            return Task.FromResult<DiscountCode?>(null);
        }

        var normalized = code.Trim().ToUpperInvariant();

        return _dbContext.DiscountCodes
            .AsTracking()
            .FirstOrDefaultAsync(discount => discount.Code == normalized, cancellationToken);
    }

    public async Task AddAsync(DiscountCode discountCode, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(discountCode);

        await _dbContext.DiscountCodes.AddAsync(discountCode, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(DiscountCode discountCode, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(discountCode);

        _dbContext.DiscountCodes.Update(discountCode);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> ExistsByCodeAsync(string code, Guid? excludeId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return false;
        }

        var normalized = code.Trim().ToUpperInvariant();

        var query = _dbContext.DiscountCodes
            .AsNoTracking()
            .Where(discount => discount.Code == normalized);

        if (excludeId.HasValue && excludeId.Value != Guid.Empty)
        {
            var excluded = excludeId.Value;
            query = query.Where(discount => discount.Id != excluded);
        }

        return await query.AnyAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<DiscountCode>> GetListAsync(CancellationToken cancellationToken)
    {
        var items = await _dbContext.DiscountCodes
            .AsNoTracking()
            .OrderBy(discount => discount.StartsAt)
            .ThenBy(discount => discount.Code)
            .ToListAsync(cancellationToken);

        return items;
    }
}

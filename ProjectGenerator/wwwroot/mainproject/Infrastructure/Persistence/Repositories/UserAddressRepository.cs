using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Attar.Application.Interfaces;
using Attar.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Attar.Infrastructure.Persistence.Repositories;

public sealed class UserAddressRepository : IUserAddressRepository
{
    private readonly AppDbContext _dbContext;

    public UserAddressRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<UserAddress?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _dbContext.UserAddresses
            .AsTracking()
            .FirstOrDefaultAsync(address => address.Id == id, cancellationToken);
    }

    public async Task<UserAddress?> GetByIdForUserAsync(Guid id, string userId, CancellationToken cancellationToken)
    {
        if (id == Guid.Empty || string.IsNullOrWhiteSpace(userId))
        {
            return null;
        }

        var normalizedUserId = userId.Trim();

        return await _dbContext.UserAddresses
            .AsTracking()
            .FirstOrDefaultAsync(
                address => address.Id == id && address.UserId == normalizedUserId && !address.IsDeleted,
                cancellationToken);
    }

    public async Task<IReadOnlyCollection<UserAddress>> GetByUserIdAsync(string userId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Array.Empty<UserAddress>();
        }

        var normalizedUserId = userId.Trim();

        return await _dbContext.UserAddresses
            .AsNoTracking()
            .Where(address => address.UserId == normalizedUserId && !address.IsDeleted)
            .OrderByDescending(address => address.IsDefault)
            .ThenByDescending(address => address.UpdateDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<UserAddress?> GetDefaultByUserIdAsync(string userId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return null;
        }

        var normalizedUserId = userId.Trim();

        return await _dbContext.UserAddresses
            .AsTracking()
            .FirstOrDefaultAsync(
                address => address.UserId == normalizedUserId && address.IsDefault && !address.IsDeleted,
                cancellationToken);
    }

    public async Task AddAsync(UserAddress address, CancellationToken cancellationToken)
    {
        await _dbContext.UserAddresses.AddAsync(address, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(UserAddress address, CancellationToken cancellationToken)
    {
        _dbContext.UserAddresses.Update(address);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveAsync(UserAddress address, CancellationToken cancellationToken)
    {
        _dbContext.UserAddresses.Update(address);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}

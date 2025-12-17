using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LogsDtoCloneTest.Application.Interfaces;
using LogsDtoCloneTest.Domain.Entities.Billing;
using Microsoft.EntityFrameworkCore;

namespace LogsDtoCloneTest.Infrastructure.Persistence.Repositories;

public sealed class WalletRepository : IWalletRepository
{
    private readonly AppDbContext _dbContext;

    public WalletRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<WalletAccount?> GetByUserIdAsync(string userId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return null;
        }

        var normalized = userId.Trim();

        return await _dbContext.WalletAccounts
            .AsTracking()
            .FirstOrDefaultAsync(account => account.UserId == normalized, cancellationToken);
    }

    public async Task<WalletAccount?> GetByUserIdWithTransactionsAsync(string userId, int? transactionsLimit, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return null;
        }

        var normalized = userId.Trim();

        IQueryable<WalletAccount> query = _dbContext.WalletAccounts.AsTracking();

        if (transactionsLimit.HasValue && transactionsLimit.Value > 0)
        {
            query = query.Include(account => account.TransactionsCollection
                .OrderByDescending(transaction => transaction.OccurredAt)
                .Take(transactionsLimit.Value));
        }
        else
        {
            query = query.Include(account => account.TransactionsCollection);
        }

        return await query.FirstOrDefaultAsync(account => account.UserId == normalized, cancellationToken);
    }

    public async Task AddAsync(WalletAccount account, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(account);

        await _dbContext.WalletAccounts.AddAsync(account, cancellationToken);
        await EnsureNewTransactionsTrackedAsync(cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(WalletAccount account, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(account);

        _dbContext.WalletAccounts.Update(account);
        await EnsureNewTransactionsTrackedAsync(cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureNewTransactionsTrackedAsync(CancellationToken cancellationToken)
    {
        foreach (var entry in _dbContext.ChangeTracker.Entries<WalletTransaction>())
        {
            if (entry.State is EntityState.Added)
            {
                continue;
            }

            if (entry.State is EntityState.Detached)
            {
                entry.State = EntityState.Added;
                continue;
            }

            var existsInDatabase = true;

            try
            {
                var databaseValues = await entry.GetDatabaseValuesAsync(cancellationToken);
                existsInDatabase = databaseValues is not null;
            }
            catch (InvalidOperationException)
            {
                existsInDatabase = false;
            }

            if (!existsInDatabase)
            {
                entry.State = EntityState.Added;
            }
        }
    }
}

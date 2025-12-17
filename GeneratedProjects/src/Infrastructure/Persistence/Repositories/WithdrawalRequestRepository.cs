using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TestAttarClone.Application.Interfaces;
using TestAttarClone.Domain.Entities.Billing;
using TestAttarClone.Domain.Enums;
using TestAttarClone.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace TestAttarClone.Infrastructure.Persistence.Repositories;

public sealed class WithdrawalRequestRepository : IWithdrawalRequestRepository
{
    private readonly AppDbContext _dbContext;

    public WithdrawalRequestRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<WithdrawalRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => await _dbContext.WithdrawalRequests
            .AsNoTracking()
            .Include(r => r.WalletTransaction)
            .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted, cancellationToken);

    public async Task<WithdrawalRequest?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken)
        => await _dbContext.WithdrawalRequests
            .Include(r => r.WalletTransaction)
            .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted, cancellationToken);

    public async Task<IReadOnlyCollection<WithdrawalRequest>> GetBySellerIdAsync(
        string sellerId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(sellerId))
        {
            return Array.Empty<WithdrawalRequest>();
        }

        var normalized = sellerId.Trim();

        return await _dbContext.WithdrawalRequests
            .AsNoTracking()
            .Include(r => r.WalletTransaction)
            .Where(r => !r.IsDeleted && r.SellerId == normalized)
            .OrderByDescending(r => r.CreateDate)
            .ToListAsync(cancellationToken);
    }
    
    public async Task<IReadOnlyCollection<WithdrawalRequest>> GetByUserIdAsync(
        string userId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Array.Empty<WithdrawalRequest>();
        }

        var normalized = userId.Trim();

        return await _dbContext.WithdrawalRequests
            .AsNoTracking()
            .Include(r => r.WalletTransaction)
            .Where(r => !r.IsDeleted && r.UserId == normalized)
            .OrderByDescending(r => r.CreateDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<WithdrawalRequest>> GetAllAsync(
        WithdrawalRequestStatus? status,
        WithdrawalRequestType? requestType,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.WithdrawalRequests
            .AsNoTracking()
            .Include(r => r.WalletTransaction)
            .Where(r => !r.IsDeleted);

        if (status.HasValue)
        {
            query = query.Where(r => r.Status == status.Value);
        }

        if (requestType.HasValue)
        {
            query = query.Where(r => r.RequestType == requestType.Value);
        }

        return await query
            .OrderByDescending(r => r.CreateDate)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(WithdrawalRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        await _dbContext.WithdrawalRequests.AddAsync(request, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(WithdrawalRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        _dbContext.WithdrawalRequests.Update(request);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}


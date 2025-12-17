using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Attar.Domain.Entities.Billing;
using Attar.Domain.Enums;

namespace Attar.Application.Interfaces;

public interface IWithdrawalRequestRepository
{
    Task<WithdrawalRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<WithdrawalRequest?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<WithdrawalRequest>> GetBySellerIdAsync(
        string sellerId,
        CancellationToken cancellationToken);
    
    Task<IReadOnlyCollection<WithdrawalRequest>> GetByUserIdAsync(
        string userId,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<WithdrawalRequest>> GetAllAsync(
        WithdrawalRequestStatus? status,
        WithdrawalRequestType? requestType,
        CancellationToken cancellationToken);

    Task AddAsync(WithdrawalRequest request, CancellationToken cancellationToken);

    Task UpdateAsync(WithdrawalRequest request, CancellationToken cancellationToken);
}


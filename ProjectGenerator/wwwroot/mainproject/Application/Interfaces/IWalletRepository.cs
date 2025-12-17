using System.Threading;
using System.Threading.Tasks;
using Attar.Domain.Entities.Billing;

namespace Attar.Application.Interfaces;

public interface IWalletRepository
{
    Task<WalletAccount?> GetByUserIdAsync(string userId, CancellationToken cancellationToken);

    Task<WalletAccount?> GetByUserIdWithTransactionsAsync(string userId, int? transactionsLimit, CancellationToken cancellationToken);

    Task AddAsync(WalletAccount account, CancellationToken cancellationToken);

    Task UpdateAsync(WalletAccount account, CancellationToken cancellationToken);
}

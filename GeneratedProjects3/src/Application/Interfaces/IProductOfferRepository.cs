using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LogTableRenameTest.Domain.Entities.Catalog;

namespace LogTableRenameTest.Application.Interfaces;

public interface IProductOfferRepository
{
    Task<ProductOffer?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<ProductOffer?> GetByProductIdAndSellerIdAsync(Guid productId, string sellerId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<ProductOffer>> GetByProductIdAsync(Guid productId, bool includeInactive = false, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ProductOffer>> GetBySellerIdAsync(string sellerId, bool includeInactive = false, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ProductOffer>> GetBySellerIdForUpdateAsync(string sellerId, bool includeInactive = false, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ProductOffer>> GetActiveOffersByProductIdAsync(Guid productId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<ProductOffer>> GetAllAsync(bool includeInactive, CancellationToken cancellationToken);

    Task AddAsync(ProductOffer offer, CancellationToken cancellationToken);

    Task UpdateAsync(ProductOffer offer, CancellationToken cancellationToken);

    Task UpdateBatchAsync(IReadOnlyCollection<ProductOffer> offers, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(Guid productId, string sellerId, CancellationToken cancellationToken);

    Task<int> GetUnpublishedActiveCountAsync(CancellationToken cancellationToken);
}


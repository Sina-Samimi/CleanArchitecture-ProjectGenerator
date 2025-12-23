using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MobiRooz.Domain.Entities.Catalog;

namespace MobiRooz.Application.Interfaces;

public interface IProductViolationReportRepository
{
    Task<ProductViolationReport?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<ProductViolationReport>> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<ProductViolationReport>> GetBySellerIdAsync(string sellerId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<ProductViolationReport>> GetByProductOfferIdAsync(Guid productOfferId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<ProductViolationReport>> GetAllAsync(CancellationToken cancellationToken, bool? isReviewed = null);

    Task<int> GetCountByProductIdAsync(Guid productId, CancellationToken cancellationToken);

    Task<int> GetCountBySellerIdAsync(string sellerId, CancellationToken cancellationToken, bool? isReviewed = null);

    Task<int> GetUnreviewedCountAsync(CancellationToken cancellationToken);

    Task AddAsync(ProductViolationReport report, CancellationToken cancellationToken);

    Task UpdateAsync(ProductViolationReport report, CancellationToken cancellationToken);
}


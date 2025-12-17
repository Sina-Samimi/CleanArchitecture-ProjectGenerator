using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TestAttarClone.Domain.Entities.Catalog;
using TestAttarClone.Domain.Enums;

namespace TestAttarClone.Application.Interfaces;

public interface IProductCustomRequestRepository
{
    Task<ProductCustomRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task AddAsync(ProductCustomRequest request, CancellationToken cancellationToken);

    Task UpdateAsync(ProductCustomRequest request, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<ProductCustomRequest>> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<ProductCustomRequest>> GetByUserIdAsync(string userId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<ProductCustomRequest>> GetByStatusAsync(CustomRequestStatus status, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<ProductCustomRequest>> GetAllAsync(int pageNumber, int pageSize, CancellationToken cancellationToken);

    Task<int> GetCountAsync(CancellationToken cancellationToken);

    Task<int> GetCountByStatusAsync(CustomRequestStatus status, CancellationToken cancellationToken);
}


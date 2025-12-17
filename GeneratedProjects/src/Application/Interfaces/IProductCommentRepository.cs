using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TestAttarClone.Domain.Entities.Catalog;

namespace TestAttarClone.Application.Interfaces;

public interface IProductCommentRepository
{
    Task<IReadOnlyCollection<ProductComment>> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken);

    Task<ProductComment?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<ProductComment>> GetPendingAsync(CancellationToken cancellationToken);

    Task AddAsync(ProductComment comment, CancellationToken cancellationToken);

    Task UpdateAsync(ProductComment comment, CancellationToken cancellationToken);
}

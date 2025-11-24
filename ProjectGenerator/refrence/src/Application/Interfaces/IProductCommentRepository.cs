using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Arsis.Domain.Entities.Catalog;

namespace Arsis.Application.Interfaces;

public interface IProductCommentRepository
{
    Task<IReadOnlyCollection<ProductComment>> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken);

    Task<ProductComment?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task UpdateAsync(ProductComment comment, CancellationToken cancellationToken);
}

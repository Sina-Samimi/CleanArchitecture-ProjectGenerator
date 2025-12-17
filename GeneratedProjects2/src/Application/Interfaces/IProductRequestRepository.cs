using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LogsDtoCloneTest.Domain.Entities.Catalog;
using LogsDtoCloneTest.Domain.Enums;

namespace LogsDtoCloneTest.Application.Interfaces;

public interface IProductRequestRepository
{
    Task<ProductRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<ProductRequest?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken);

    Task AddAsync(ProductRequest request, CancellationToken cancellationToken);

    Task UpdateAsync(ProductRequest request, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<ProductRequest>> GetBySellerIdAsync(string sellerId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<ProductRequest>> GetByStatusAsync(ProductRequestStatus status, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<ProductRequest>> GetAllAsync(int pageNumber, int pageSize, CancellationToken cancellationToken);

    Task<int> GetCountAsync(CancellationToken cancellationToken);

    Task<int> GetCountByStatusAsync(ProductRequestStatus status, CancellationToken cancellationToken);

    Task<bool> ExistsBySlugAsync(string slug, Guid? excludeId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<ProductRequest>> GetByTargetProductIdAsync(Guid targetProductId, CancellationToken cancellationToken);
}


using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Arsis.Application.DTOs.Catalog;
using Arsis.Domain.Entities.Catalog;

namespace Arsis.Application.Interfaces;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<Product?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken);

    Task AddAsync(Product product, CancellationToken cancellationToken);

    Task UpdateAsync(Product product, CancellationToken cancellationToken);

    Task RemoveAsync(Product product, CancellationToken cancellationToken);

    Task<ProductListResultDto> GetListAsync(ProductListFilterDto filter, IReadOnlyCollection<Guid>? categoryIds, CancellationToken cancellationToken);

    Task<bool> ExistsInCategoriesAsync(IReadOnlyCollection<Guid> categoryIds, CancellationToken cancellationToken);

    Task<bool> ExistsBySlugAsync(string slug, Guid? excludeId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<Product>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<string>> GetAllTagsAsync(CancellationToken cancellationToken);

    Task<IReadOnlyCollection<ProductListItemDto>> GetByTeacherAsync(string teacherId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<ProductExecutionStepSummaryDto>> GetExecutionStepSummariesAsync(CancellationToken cancellationToken);
}

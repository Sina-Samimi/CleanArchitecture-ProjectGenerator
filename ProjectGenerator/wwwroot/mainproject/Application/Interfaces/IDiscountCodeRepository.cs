using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Attar.Domain.Entities.Discounts;

namespace Attar.Application.Interfaces;

public interface IDiscountCodeRepository
{
    Task<DiscountCode?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<DiscountCode?> GetByCodeAsync(string code, CancellationToken cancellationToken);

    Task AddAsync(DiscountCode discountCode, CancellationToken cancellationToken);

    Task UpdateAsync(DiscountCode discountCode, CancellationToken cancellationToken);

    Task<bool> ExistsByCodeAsync(string code, Guid? excludeId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<DiscountCode>> GetListAsync(CancellationToken cancellationToken);
}

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arsis.Application.Abstractions.Messaging;
using Arsis.Application.DTOs.Discounts;
using Arsis.Application.Interfaces;
using Arsis.SharedKernel.BaseTypes;

namespace Arsis.Application.Queries.Discounts;

public sealed record GetDiscountCodeListQuery() : IQuery<IReadOnlyCollection<DiscountCodeDetailDto>>
{
    public sealed class Handler : IQueryHandler<GetDiscountCodeListQuery, IReadOnlyCollection<DiscountCodeDetailDto>>
    {
        private readonly IDiscountCodeRepository _discountRepository;

        public Handler(IDiscountCodeRepository discountRepository)
        {
            _discountRepository = discountRepository;
        }

        public async Task<Result<IReadOnlyCollection<DiscountCodeDetailDto>>> Handle(GetDiscountCodeListQuery request, CancellationToken cancellationToken)
        {
            var items = await _discountRepository.GetListAsync(cancellationToken);
            var mapped = items.Select(discount => discount.ToDetailDto()).ToArray();
            return Result<IReadOnlyCollection<DiscountCodeDetailDto>>.Success(mapped);
        }
    }
}

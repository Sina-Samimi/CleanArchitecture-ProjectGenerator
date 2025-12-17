using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LogsDtoCloneTest.Application.Abstractions.Messaging;
using LogsDtoCloneTest.Application.DTOs.Discounts;
using LogsDtoCloneTest.Application.Interfaces;
using LogsDtoCloneTest.SharedKernel.BaseTypes;

namespace LogsDtoCloneTest.Application.Queries.Discounts;

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

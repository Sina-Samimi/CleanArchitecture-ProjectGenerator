using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LogTableRenameTest.Application.Abstractions.Messaging;
using LogTableRenameTest.Application.DTOs.Discounts;
using LogTableRenameTest.Application.Interfaces;
using LogTableRenameTest.SharedKernel.BaseTypes;

namespace LogTableRenameTest.Application.Queries.Discounts;

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

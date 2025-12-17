using System;
using System.Threading;
using System.Threading.Tasks;
using LogsDtoCloneTest.Application.Abstractions.Messaging;
using LogsDtoCloneTest.Application.DTOs.Discounts;
using LogsDtoCloneTest.Application.Interfaces;
using LogsDtoCloneTest.SharedKernel.BaseTypes;

namespace LogsDtoCloneTest.Application.Queries.Discounts;

public sealed record GetDiscountCodeDetailsQuery(Guid Id) : IQuery<DiscountCodeDetailDto>
{
    public sealed class Handler : IQueryHandler<GetDiscountCodeDetailsQuery, DiscountCodeDetailDto>
    {
        private readonly IDiscountCodeRepository _discountRepository;

        public Handler(IDiscountCodeRepository discountRepository)
        {
            _discountRepository = discountRepository;
        }

        public async Task<Result<DiscountCodeDetailDto>> Handle(GetDiscountCodeDetailsQuery request, CancellationToken cancellationToken)
        {
            var discountCode = await _discountRepository.GetByIdAsync(request.Id, cancellationToken);
            if (discountCode is null)
            {
                return Result<DiscountCodeDetailDto>.Failure("کد تخفیف یافت نشد.");
            }

            return Result<DiscountCodeDetailDto>.Success(discountCode.ToDetailDto());
        }
    }
}

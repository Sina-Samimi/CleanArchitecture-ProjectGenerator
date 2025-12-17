using System;
using System.Threading;
using System.Threading.Tasks;
using LogTableRenameTest.Application.Abstractions.Messaging;
using LogTableRenameTest.Application.DTOs.Discounts;
using LogTableRenameTest.Application.Interfaces;
using LogTableRenameTest.SharedKernel.BaseTypes;

namespace LogTableRenameTest.Application.Queries.Discounts;

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

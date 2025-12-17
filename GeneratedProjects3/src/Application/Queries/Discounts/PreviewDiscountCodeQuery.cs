using System;
using System.Threading;
using System.Threading.Tasks;
using LogTableRenameTest.Application.Abstractions.Messaging;
using LogTableRenameTest.Application.DTOs.Discounts;
using LogTableRenameTest.Application.Interfaces;
using LogTableRenameTest.Domain.Exceptions;
using LogTableRenameTest.SharedKernel.BaseTypes;

namespace LogTableRenameTest.Application.Queries.Discounts;

public sealed record PreviewDiscountCodeQuery(
    string Code,
    decimal OriginalPrice,
    string? AudienceKey,
    DateTimeOffset? EvaluationDate) : IQuery<DiscountApplicationResultDto>
{
    public sealed class Handler : IQueryHandler<PreviewDiscountCodeQuery, DiscountApplicationResultDto>
    {
        private readonly IDiscountCodeRepository _discountRepository;

        public Handler(IDiscountCodeRepository discountRepository)
        {
            _discountRepository = discountRepository;
        }

        public async Task<Result<DiscountApplicationResultDto>> Handle(PreviewDiscountCodeQuery request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Code))
            {
                return Result<DiscountApplicationResultDto>.Failure("کد تخفیف ارسال نشده است.");
            }

            if (request.OriginalPrice < 0)
            {
                return Result<DiscountApplicationResultDto>.Failure("مبلغ سفارش نمی‌تواند منفی باشد.");
            }

            var discountCode = await _discountRepository.GetByCodeAsync(request.Code.Trim().ToUpperInvariant(), cancellationToken);
            if (discountCode is null)
            {
                return Result<DiscountApplicationResultDto>.Failure("کد تخفیف یافت نشد.");
            }

            try
            {
                var evaluationDate = request.EvaluationDate ?? DateTimeOffset.UtcNow;
                var result = discountCode.Preview(request.OriginalPrice, evaluationDate, request.AudienceKey);
                return Result<DiscountApplicationResultDto>.Success(result.ToDto());
            }
            catch (DomainException ex)
            {
                return Result<DiscountApplicationResultDto>.Failure(ex.Message);
            }
        }
    }
}

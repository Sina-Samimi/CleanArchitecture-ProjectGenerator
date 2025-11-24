using System;
using System.Threading;
using System.Threading.Tasks;
using Arsis.Application.Abstractions.Messaging;
using Arsis.Application.DTOs.Discounts;
using Arsis.Application.Interfaces;
using Arsis.Domain.Exceptions;
using Arsis.SharedKernel.BaseTypes;

namespace Arsis.Application.Commands.Discounts;

public sealed record RedeemDiscountCodeCommand(
    string Code,
    decimal OriginalPrice,
    string? AudienceKey) : ICommand<DiscountApplicationResultDto>
{
    public sealed class Handler : ICommandHandler<RedeemDiscountCodeCommand, DiscountApplicationResultDto>
    {
        private readonly IDiscountCodeRepository _discountRepository;
        private readonly IAuditContext _auditContext;

        public Handler(IDiscountCodeRepository discountRepository, IAuditContext auditContext)
        {
            _discountRepository = discountRepository;
            _auditContext = auditContext;
        }

        public async Task<Result<DiscountApplicationResultDto>> Handle(RedeemDiscountCodeCommand request, CancellationToken cancellationToken)
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
                var audit = _auditContext.Capture();
                var result = discountCode.Redeem(request.OriginalPrice, audit.Timestamp, request.AudienceKey);
                discountCode.UpdaterId = audit.UserId;
                discountCode.Ip = audit.IpAddress;

                await _discountRepository.UpdateAsync(discountCode, cancellationToken);

                return Result<DiscountApplicationResultDto>.Success(result.ToDto());
            }
            catch (DomainException ex)
            {
                return Result<DiscountApplicationResultDto>.Failure(ex.Message);
            }
        }
    }
}

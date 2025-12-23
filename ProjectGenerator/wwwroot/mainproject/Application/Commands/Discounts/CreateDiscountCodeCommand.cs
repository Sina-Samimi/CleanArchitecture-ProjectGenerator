using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MobiRooz.Application.Abstractions.Messaging;
using MobiRooz.Application.Interfaces;
using MobiRooz.Domain.Entities.Discounts;
using MobiRooz.Domain.Enums;
using MobiRooz.Domain.Exceptions;
using MobiRooz.SharedKernel.BaseTypes;

namespace MobiRooz.Application.Commands.Discounts;

public sealed record CreateDiscountCodeCommand(
    string Code,
    string Name,
    string? Description,
    DiscountType DiscountType,
    decimal DiscountValue,
    DateTimeOffset StartsAt,
    DateTimeOffset? EndsAt,
    decimal? MaxDiscountAmount,
    decimal? MinimumOrderAmount,
    bool IsActive,
    int? GlobalUsageLimit,
    IReadOnlyCollection<CreateDiscountCodeCommand.GroupRule>? GroupRules) : ICommand<Guid>
{
    public sealed record GroupRule(
        string Key,
        int? UsageLimit,
        DiscountType? DiscountTypeOverride,
        decimal? DiscountValueOverride,
        decimal? MaxDiscountAmountOverride,
        decimal? MinimumOrderAmountOverride);

    public sealed class Handler : ICommandHandler<CreateDiscountCodeCommand, Guid>
    {
        private readonly IDiscountCodeRepository _discountRepository;
        private readonly IAuditContext _auditContext;

        public Handler(IDiscountCodeRepository discountRepository, IAuditContext auditContext)
        {
            _discountRepository = discountRepository;
            _auditContext = auditContext;
        }

        public async Task<Result<Guid>> Handle(CreateDiscountCodeCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Code))
            {
                return Result<Guid>.Failure("کد تخفیف الزامی است.");
            }

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return Result<Guid>.Failure("عنوان کد تخفیف الزامی است.");
            }

            if (request.DiscountValue <= 0)
            {
                return Result<Guid>.Failure("مقدار تخفیف باید بیشتر از صفر باشد.");
            }

            var normalizedCode = request.Code.Trim().ToUpperInvariant();

            var exists = await _discountRepository.ExistsByCodeAsync(normalizedCode, null, cancellationToken);
            if (exists)
            {
                return Result<Guid>.Failure("این کد تخفیف قبلاً ثبت شده است.");
            }

            try
            {
                var groupConfigurations = request.GroupRules?
                    .Select(rule => new DiscountGroupConfiguration(
                        rule.Key,
                        rule.UsageLimit,
                        rule.DiscountTypeOverride,
                        rule.DiscountValueOverride,
                        rule.MaxDiscountAmountOverride,
                        rule.MinimumOrderAmountOverride))
                    .ToArray();

                var discountCode = new DiscountCode(
                    normalizedCode,
                    request.Name,
                    request.Description,
                    request.DiscountType,
                    request.DiscountValue,
                    request.StartsAt,
                    request.EndsAt,
                    request.MaxDiscountAmount,
                    request.MinimumOrderAmount,
                    request.IsActive,
                    request.GlobalUsageLimit,
                    groupConfigurations);

                var audit = _auditContext.Capture();
                discountCode.CreatorId = audit.UserId;
                discountCode.CreateDate = audit.Timestamp;
                discountCode.UpdateDate = audit.Timestamp;
                discountCode.Ip = audit.IpAddress;

                await _discountRepository.AddAsync(discountCode, cancellationToken);

                return Result<Guid>.Success(discountCode.Id);
            }
            catch (DomainException ex)
            {
                return Result<Guid>.Failure(ex.Message);
            }
        }
    }
}

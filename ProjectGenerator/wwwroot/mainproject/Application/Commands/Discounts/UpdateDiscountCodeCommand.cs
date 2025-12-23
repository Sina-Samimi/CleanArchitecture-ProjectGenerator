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

public sealed record UpdateDiscountCodeCommand(
    Guid Id,
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
    IReadOnlyCollection<UpdateDiscountCodeCommand.GroupRule>? GroupRules) : ICommand
{
    public sealed record GroupRule(
        string Key,
        int? UsageLimit,
        DiscountType? DiscountTypeOverride,
        decimal? DiscountValueOverride,
        decimal? MaxDiscountAmountOverride,
        decimal? MinimumOrderAmountOverride);

    public sealed class Handler : ICommandHandler<UpdateDiscountCodeCommand>
    {
        private readonly IDiscountCodeRepository _discountRepository;
        private readonly IAuditContext _auditContext;

        public Handler(IDiscountCodeRepository discountRepository, IAuditContext auditContext)
        {
            _discountRepository = discountRepository;
            _auditContext = auditContext;
        }

        public async Task<Result> Handle(UpdateDiscountCodeCommand request, CancellationToken cancellationToken)
        {
            if (request.Id == Guid.Empty)
            {
                return Result.Failure("شناسه کد تخفیف معتبر نیست.");
            }

            if (string.IsNullOrWhiteSpace(request.Code))
            {
                return Result.Failure("کد تخفیف الزامی است.");
            }

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return Result.Failure("عنوان کد تخفیف الزامی است.");
            }

            if (request.DiscountValue <= 0)
            {
                return Result.Failure("مقدار تخفیف باید بیشتر از صفر باشد.");
            }

            var discount = await _discountRepository.GetByIdAsync(request.Id, cancellationToken);
            if (discount is null)
            {
                return Result.Failure("کد تخفیف مورد نظر یافت نشد.");
            }

            var normalizedCode = request.Code.Trim().ToUpperInvariant();
            var exists = await _discountRepository.ExistsByCodeAsync(normalizedCode, request.Id, cancellationToken);
            if (exists)
            {
                return Result.Failure("این کد تخفیف قبلاً ثبت شده است.");
            }

            try
            {
                discount.SetCode(normalizedCode);
                discount.UpdateDetails(request.Name, request.Description);
                discount.SetDiscount(
                    request.DiscountType,
                    request.DiscountValue,
                    request.MaxDiscountAmount,
                    request.MinimumOrderAmount);
                discount.UpdateSchedule(request.StartsAt, request.EndsAt);
                discount.SetGlobalUsageLimit(request.GlobalUsageLimit);

                if (request.IsActive)
                {
                    discount.Activate();
                }
                else
                {
                    discount.Deactivate();
                }

                UpdateGroups(discount, request.GroupRules);

                var audit = _auditContext.Capture();
                discount.UpdaterId = audit.UserId;
                discount.UpdateDate = audit.Timestamp;
                discount.Ip = audit.IpAddress;

                await _discountRepository.UpdateAsync(discount, cancellationToken);

                return Result.Success();
            }
            catch (DomainException ex)
            {
                return Result.Failure(ex.Message);
            }
        }

        private static void UpdateGroups(
            DiscountCode discountCode,
            IReadOnlyCollection<GroupRule>? requestGroups)
        {
            if (requestGroups is null || requestGroups.Count == 0)
            {
                discountCode.ClearGroups();
                return;
            }

            var desiredConfigurations = requestGroups
                .Select(rule => new DiscountGroupConfiguration(
                    rule.Key,
                    rule.UsageLimit,
                    rule.DiscountTypeOverride,
                    rule.DiscountValueOverride,
                    rule.MaxDiscountAmountOverride,
                    rule.MinimumOrderAmountOverride))
                .ToArray();

            var desiredKeys = new HashSet<string>(
                desiredConfigurations.Select(configuration => configuration.Key),
                StringComparer.OrdinalIgnoreCase);

            var existingKeys = discountCode.GroupConfigurations
                .Select(configuration => configuration.Key)
                .ToArray();

            foreach (var key in existingKeys)
            {
                if (!desiredKeys.Contains(key))
                {
                    discountCode.RemoveGroup(key);
                }
            }

            foreach (var configuration in desiredConfigurations)
            {
                discountCode.AddOrUpdateGroup(configuration);
            }
        }
    }
}

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TestAttarClone.Application.Abstractions.Messaging;
using TestAttarClone.Application.DTOs.Cart;
using TestAttarClone.Application.Interfaces;
using TestAttarClone.Domain.Exceptions;
using TestAttarClone.SharedKernel.BaseTypes;

namespace TestAttarClone.Application.Commands.Cart;

public sealed record ApplyCartDiscountCommand(
    string? UserId,
    Guid? AnonymousId,
    string DiscountCode) : ICommand<CartDto>
{
    public sealed class Handler : ICommandHandler<ApplyCartDiscountCommand, CartDto>
    {
        private readonly IShoppingCartRepository _cartRepository;
        private readonly IDiscountCodeRepository _discountRepository;
        private readonly IAuditContext _auditContext;

        public Handler(
            IShoppingCartRepository cartRepository,
            IDiscountCodeRepository discountRepository,
            IAuditContext auditContext)
        {
            _cartRepository = cartRepository;
            _discountRepository = discountRepository;
            _auditContext = auditContext;
        }

        public async Task<Result<CartDto>> Handle(ApplyCartDiscountCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.DiscountCode))
            {
                return Result<CartDto>.Failure("کد تخفیف را وارد کنید.");
            }

            var cart = await ResolveCartAsync(request.UserId, request.AnonymousId, cancellationToken);
            if (cart is null || cart.IsEmpty)
            {
                return Result<CartDto>.Failure("سبد خرید شما خالی است.");
            }

            var discount = await _discountRepository.GetByCodeAsync(request.DiscountCode.Trim(), cancellationToken);
            if (discount is null)
            {
                return Result<CartDto>.Failure("کد تخفیف معتبر نیست یا منقضی شده است.");
            }

            var audit = _auditContext.Capture();
            var audienceKey = !string.IsNullOrWhiteSpace(request.UserId) ? request.UserId : null;

            // Check if user has already used this discount code
            if (!string.IsNullOrWhiteSpace(audienceKey))
            {
                // Check usage limit for this user group
                var remainingUses = discount.GetRemainingUsesForGroup(audienceKey);
                if (remainingUses.HasValue && remainingUses.Value <= 0)
                {
                    return Result<CartDto>.Failure("شما قبلاً از این کد تخفیف استفاده کرده‌اید.");
                }
            }

            // Check global usage limit
            if (discount.GlobalUsageLimit is not null && discount.TotalRedemptions >= discount.GlobalUsageLimit.Value)
            {
                return Result<CartDto>.Failure("سقف استفاده از این کد تخفیف به پایان رسیده است.");
            }

            try
            {
                cart.ApplyDiscount(discount, audit.Timestamp, audienceKey);
            }
            catch (DomainException ex)
            {
                return Result<CartDto>.Failure(ex.Message);
            }
            catch (Exception)
            {
                return Result<CartDto>.Failure("امکان اعمال این کد تخفیف روی سبد خرید وجود ندارد.");
            }

            // Only set UpdaterId and Ip here, UpdateDate will be set by AuditInterceptor
            cart.UpdaterId = audit.UserId;
            cart.Ip = audit.IpAddress;

            await _cartRepository.UpdateAsync(cart, cancellationToken);
            return Result<CartDto>.Success(cart.ToDto());
        }

        private async Task<Domain.Entities.Orders.ShoppingCart?> ResolveCartAsync(
            string? userId,
            Guid? anonymousId,
            CancellationToken cancellationToken)
        {
            var normalizedUserId = string.IsNullOrWhiteSpace(userId) ? null : userId.Trim();
            if (!string.IsNullOrWhiteSpace(normalizedUserId))
            {
                var cart = await _cartRepository.GetByUserIdAsync(normalizedUserId, cancellationToken);
                if (cart is not null)
                {
                    return cart;
                }
            }

            if (anonymousId is null || anonymousId.Value == Guid.Empty)
            {
                return null;
            }

            return await _cartRepository.GetByAnonymousIdAsync(anonymousId.Value, cancellationToken);
        }
    }
}

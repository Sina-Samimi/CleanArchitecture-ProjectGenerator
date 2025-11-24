using System;
using System.Threading;
using System.Threading.Tasks;
using Arsis.Application.Abstractions.Messaging;
using Arsis.Application.DTOs.Cart;
using Arsis.Application.Interfaces;
using Arsis.SharedKernel.BaseTypes;

namespace Arsis.Application.Commands.Cart;

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

            try
            {
                cart.ApplyDiscount(discount, audit.Timestamp, audienceKey);
            }
            catch (Exception)
            {
                return Result<CartDto>.Failure("امکان اعمال این کد تخفیف روی سبد خرید وجود ندارد.");
            }

            cart.UpdaterId = audit.UserId;
            cart.UpdateDate = audit.Timestamp;
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

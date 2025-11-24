using System;
using System.Threading;
using System.Threading.Tasks;
using Arsis.Application.Abstractions.Messaging;
using Arsis.Application.DTOs.Cart;
using Arsis.Application.Interfaces;
using Arsis.SharedKernel.BaseTypes;

namespace Arsis.Application.Commands.Cart;

public sealed record RemoveCartItemCommand(
    string? UserId,
    Guid? AnonymousId,
    Guid ProductId) : ICommand<CartDto>
{
    public sealed class Handler : ICommandHandler<RemoveCartItemCommand, CartDto>
    {
        private readonly IShoppingCartRepository _cartRepository;
        private readonly IAuditContext _auditContext;

        public Handler(IShoppingCartRepository cartRepository, IAuditContext auditContext)
        {
            _cartRepository = cartRepository;
            _auditContext = auditContext;
        }

        public async Task<Result<CartDto>> Handle(RemoveCartItemCommand request, CancellationToken cancellationToken)
        {
            if (request.ProductId == Guid.Empty)
            {
                return Result<CartDto>.Failure("محصول انتخاب شده معتبر نیست.");
            }

            var cart = await ResolveCartAsync(request.UserId, request.AnonymousId, cancellationToken);
            if (cart is null)
            {
                return Result<CartDto>.Failure("سبد خریدی برای بروزرسانی یافت نشد.");
            }

            var removed = cart.RemoveItem(request.ProductId);
            if (!removed)
            {
                return Result<CartDto>.Failure("محصول مورد نظر در سبد خرید وجود ندارد.");
            }

            var audit = _auditContext.Capture();
            cart.UpdaterId = audit.UserId;
            cart.UpdateDate = audit.Timestamp;
            cart.Ip = audit.IpAddress;

            if (cart.IsEmpty)
            {
                await _cartRepository.RemoveAsync(cart, cancellationToken);
                return Result<CartDto>.Success(CartDtoMapper.CreateEmpty(cart.AnonymousId, cart.UserId));
            }

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

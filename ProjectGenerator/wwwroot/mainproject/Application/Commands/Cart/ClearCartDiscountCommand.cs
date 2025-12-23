using System;
using System.Threading;
using System.Threading.Tasks;
using MobiRooz.Application.Abstractions.Messaging;
using MobiRooz.Application.DTOs.Cart;
using MobiRooz.Application.Interfaces;
using MobiRooz.SharedKernel.BaseTypes;

namespace MobiRooz.Application.Commands.Cart;

public sealed record ClearCartDiscountCommand(
    string? UserId,
    Guid? AnonymousId) : ICommand<CartDto>
{
    public sealed class Handler : ICommandHandler<ClearCartDiscountCommand, CartDto>
    {
        private readonly IShoppingCartRepository _cartRepository;
        private readonly IAuditContext _auditContext;

        public Handler(IShoppingCartRepository cartRepository, IAuditContext auditContext)
        {
            _cartRepository = cartRepository;
            _auditContext = auditContext;
        }

        public async Task<Result<CartDto>> Handle(ClearCartDiscountCommand request, CancellationToken cancellationToken)
        {
            var cart = await ResolveCartAsync(request.UserId, request.AnonymousId, cancellationToken);
            if (cart is null)
            {
                return Result<CartDto>.Failure("سبد خریدی برای بروزرسانی یافت نشد.");
            }

            cart.ClearDiscount();

            var audit = _auditContext.Capture();
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

using System;
using System.Threading;
using System.Threading.Tasks;
using TestAttarClone.Application.Abstractions.Messaging;
using TestAttarClone.Application.DTOs.Cart;
using TestAttarClone.Application.Interfaces;
using TestAttarClone.SharedKernel.BaseTypes;

namespace TestAttarClone.Application.Queries.Cart;

public sealed record GetCartQuery(string? UserId, Guid? AnonymousId) : IQuery<CartDto>
{
    public sealed class Handler : IQueryHandler<GetCartQuery, CartDto>
    {
        private readonly IShoppingCartRepository _cartRepository;

        public Handler(IShoppingCartRepository cartRepository)
        {
            _cartRepository = cartRepository;
        }

        public async Task<Result<CartDto>> Handle(GetCartQuery request, CancellationToken cancellationToken)
        {
            var normalizedUserId = string.IsNullOrWhiteSpace(request.UserId) ? null : request.UserId.Trim();

            if (!string.IsNullOrWhiteSpace(normalizedUserId))
            {
                var userCart = await _cartRepository.GetByUserIdAsync(normalizedUserId, cancellationToken);
                if (userCart is not null)
                {
                    userCart.EnsureDiscountMatchesSubtotal();
                    return Result<CartDto>.Success(userCart.ToDto());
                }
            }

            if (request.AnonymousId is null || request.AnonymousId.Value == Guid.Empty)
            {
                return Result<CartDto>.Success(CartDtoMapper.CreateEmpty(request.AnonymousId, normalizedUserId));
            }

            var anonymousCart = await _cartRepository.GetByAnonymousIdAsync(request.AnonymousId.Value, cancellationToken);
            if (anonymousCart is null)
            {
                return Result<CartDto>.Success(CartDtoMapper.CreateEmpty(request.AnonymousId, normalizedUserId));
            }

            anonymousCart.EnsureDiscountMatchesSubtotal();
            return Result<CartDto>.Success(anonymousCart.ToDto());
        }
    }
}

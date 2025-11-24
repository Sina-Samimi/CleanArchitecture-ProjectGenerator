using System;
using System.Threading;
using System.Threading.Tasks;
using Arsis.Application.Abstractions.Messaging;
using Arsis.Application.DTOs.Cart;
using Arsis.Application.Interfaces;
using Arsis.SharedKernel.BaseTypes;

namespace Arsis.Application.Commands.Cart;

public sealed record UpdateCartItemQuantityCommand(
    string? UserId,
    Guid? AnonymousId,
    Guid ProductId,
    int Quantity) : ICommand<CartDto>
{
    public sealed class Handler : ICommandHandler<UpdateCartItemQuantityCommand, CartDto>
    {
        private readonly IShoppingCartRepository _cartRepository;
        private readonly IProductRepository _productRepository;
        private readonly IAuditContext _auditContext;

        public Handler(
            IShoppingCartRepository cartRepository,
            IProductRepository productRepository,
            IAuditContext auditContext)
        {
            _cartRepository = cartRepository;
            _productRepository = productRepository;
            _auditContext = auditContext;
        }

        public async Task<Result<CartDto>> Handle(UpdateCartItemQuantityCommand request, CancellationToken cancellationToken)
        {
            if (request.ProductId == Guid.Empty)
            {
                return Result<CartDto>.Failure("محصول انتخاب شده معتبر نیست.");
            }

            if (request.Quantity <= 0)
            {
                return Result<CartDto>.Failure("تعداد باید بزرگتر از صفر باشد.");
            }

            var cart = await ResolveCartAsync(request.UserId, request.AnonymousId, cancellationToken);
            if (cart is null)
            {
                return Result<CartDto>.Failure("سبد خریدی برای بروزرسانی یافت نشد.");
            }

            var product = await _productRepository.GetByIdAsync(request.ProductId, cancellationToken);
            if (product is null)
            {
                return Result<CartDto>.Failure("محصول مورد نظر یافت نشد.");
            }

            cart.SetItemQuantity(
                product.Id,
                product.Name,
                product.SeoSlug,
                product.Price,
                product.CompareAtPrice,
                product.FeaturedImagePath,
                product.Type,
                request.Quantity);

            var audit = _auditContext.Capture();
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

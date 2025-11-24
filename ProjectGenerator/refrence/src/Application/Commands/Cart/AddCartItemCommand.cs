using System;
using System.Threading;
using System.Threading.Tasks;
using Arsis.Application.Abstractions.Messaging;
using Arsis.Application.DTOs.Cart;
using Arsis.Application.Interfaces;
using Arsis.Domain.Constants;
using Arsis.Domain.Entities.Catalog;
using Arsis.Domain.Entities.Orders;
using Arsis.SharedKernel.BaseTypes;

namespace Arsis.Application.Commands.Cart;

public sealed record AddCartItemCommand(
    string? UserId,
    Guid? AnonymousId,
    Guid ProductId,
    int Quantity) : ICommand<CartDto>
{
    public sealed class Handler : ICommandHandler<AddCartItemCommand, CartDto>
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

        public async Task<Result<CartDto>> Handle(AddCartItemCommand request, CancellationToken cancellationToken)
        {
            if (request.ProductId == Guid.Empty)
            {
                return Result<CartDto>.Failure("محصول انتخاب شده معتبر نیست.");
            }

            if (request.Quantity <= 0)
            {
                return Result<CartDto>.Failure("تعداد باید بزرگتر از صفر باشد.");
            }

            ShoppingCart? cart = null;
            var cartWasCreated = false;
            var normalizedUserId = string.IsNullOrWhiteSpace(request.UserId) ? null : request.UserId.Trim();

            if (!string.IsNullOrWhiteSpace(normalizedUserId))
            {
                cart = await _cartRepository.GetByUserIdAsync(normalizedUserId, cancellationToken);
            }

            var effectiveAnonymousId = request.AnonymousId;
            if (cart is null && effectiveAnonymousId is not null && effectiveAnonymousId.Value != Guid.Empty)
            {
                cart = await _cartRepository.GetByAnonymousIdAsync(effectiveAnonymousId.Value, cancellationToken);
            }

            if (cart is null)
            {
                if (!string.IsNullOrWhiteSpace(normalizedUserId))
                {
                    cart = ShoppingCart.CreateForUser(normalizedUserId);
                    cartWasCreated = true;
                }
                else
                {
                    effectiveAnonymousId = effectiveAnonymousId is null || effectiveAnonymousId.Value == Guid.Empty
                        ? Guid.NewGuid()
                        : effectiveAnonymousId;
                    cart = ShoppingCart.CreateForAnonymous(effectiveAnonymousId.Value);
                    cartWasCreated = true;
                }
            }

            Product? product = await _productRepository.GetByIdAsync(request.ProductId, cancellationToken);
            if (product is null || product.IsDeleted || !product.IsPublished)
            {
                return Result<CartDto>.Failure("محصول مورد نظر در دسترس نیست.");
            }

            if (product.TrackInventory && product.StockQuantity <= 0)
            {
                return Result<CartDto>.Failure("این محصول موجودی کافی ندارد.");
            }

            var item = cart.AddItem(
                product.Id,
                product.Name,
                product.SeoSlug,
                product.Price,
                product.CompareAtPrice,
                product.FeaturedImagePath,
                product.Type,
                request.Quantity);

            var audit = _auditContext.Capture();

            if (cartWasCreated || cart.CreatorId == SystemUsers.AutomationId)
            {
                cart.CreatorId = audit.UserId;
                cart.CreateDate = audit.Timestamp;
            }

            cart.UpdaterId = audit.UserId;
            cart.UpdateDate = audit.Timestamp;
            cart.Ip = audit.IpAddress;

            if (item.CreatorId == SystemUsers.AutomationId || cartWasCreated)
            {
                item.CreatorId = audit.UserId;
                item.CreateDate = audit.Timestamp;
            }

            item.UpdaterId = audit.UserId;
            item.UpdateDate = audit.Timestamp;
            item.Ip = audit.IpAddress;

            if (cartWasCreated)
            {
                await _cartRepository.AddAsync(cart, cancellationToken);
            }
            else
            {
                await _cartRepository.UpdateAsync(cart, cancellationToken);
            }

            var dto = cart.ToDto();

            if (dto.AnonymousId is null && cart.AnonymousId is not null)
            {
                dto = dto with { AnonymousId = cart.AnonymousId };
            }

            return Result<CartDto>.Success(dto);
        }
    }
}

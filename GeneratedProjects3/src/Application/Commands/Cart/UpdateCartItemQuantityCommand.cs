using System;
using System.Threading;
using System.Threading.Tasks;
using LogTableRenameTest.Application.Abstractions.Messaging;
using LogTableRenameTest.Application.DTOs.Cart;
using LogTableRenameTest.Application.Interfaces;
using LogTableRenameTest.Domain.Exceptions;
using LogTableRenameTest.SharedKernel.BaseTypes;
using Microsoft.Extensions.Logging;

namespace LogTableRenameTest.Application.Commands.Cart;

public sealed record UpdateCartItemQuantityCommand(
    string? UserId,
    Guid? AnonymousId,
    Guid ProductId,
    int Quantity,
    Guid? VariantId = null,
    Guid? OfferId = null) : ICommand<CartDto>
{
    public sealed class Handler : ICommandHandler<UpdateCartItemQuantityCommand, CartDto>
    {
        private readonly IShoppingCartRepository _cartRepository;
        private readonly IProductRepository _productRepository;
        private readonly IProductOfferRepository _productOfferRepository;
        private readonly IAuditContext _auditContext;
        private readonly ILogger<Handler> _logger;

        public Handler(
            IShoppingCartRepository cartRepository,
            IProductRepository productRepository,
            IProductOfferRepository productOfferRepository,
            IAuditContext auditContext,
            ILogger<Handler> logger)
        {
            _cartRepository = cartRepository;
            _productRepository = productRepository;
            _productOfferRepository = productOfferRepository;
            _auditContext = auditContext;
            _logger = logger;
        }

        public async Task<Result<CartDto>> Handle(UpdateCartItemQuantityCommand request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("UpdateCartItemQuantityCommand.Handle started - ProductId: {ProductId}, Quantity: {Quantity}, VariantId: {VariantId}", 
                    request.ProductId, request.Quantity, request.VariantId);
                
                if (request.ProductId == Guid.Empty)
                {
                    _logger.LogWarning("UpdateCartItemQuantityCommand - Invalid ProductId (Empty)");
                    return Result<CartDto>.Failure("محصول انتخاب شده معتبر نیست.");
                }

                if (request.Quantity <= 0)
                {
                    _logger.LogWarning("UpdateCartItemQuantityCommand - Invalid Quantity: {Quantity}", request.Quantity);
                    return Result<CartDto>.Failure("تعداد باید بزرگتر از صفر باشد.");
                }

                _logger.LogDebug("UpdateCartItemQuantityCommand - Resolving cart - UserId: {UserId}, AnonymousId: {AnonymousId}", 
                    request.UserId ?? "null", request.AnonymousId);
                
                var cart = await ResolveCartAsync(request.UserId, request.AnonymousId, cancellationToken);
                if (cart is null)
                {
                    _logger.LogWarning("UpdateCartItemQuantityCommand - Cart not found - UserId: {UserId}, AnonymousId: {AnonymousId}", 
                        request.UserId ?? "null", request.AnonymousId);
                    return Result<CartDto>.Failure("سبد خریدی برای بروزرسانی یافت نشد.");
                }

                // Find the existing cart item to get its variantId if not provided
                var existingCartItem = cart.Items.FirstOrDefault(item => 
                    item.ProductId == request.ProductId && 
                    (request.VariantId.HasValue ? item.VariantId == request.VariantId : true) &&
                    (request.OfferId.HasValue ? item.OfferId == request.OfferId : true));
                
                var variantIdToUse = request.VariantId;
                if (!variantIdToUse.HasValue && existingCartItem != null)
                {
                    // Use the variantId from the existing cart item if not provided in request
                    variantIdToUse = existingCartItem.VariantId;
                    _logger.LogDebug("UpdateCartItemQuantityCommand - Using variantId from existing cart item: {VariantId}", variantIdToUse);
                }
                else if (existingCartItem == null)
                {
                    // Try to find by ProductId only (for backward compatibility)
                    existingCartItem = cart.Items.FirstOrDefault(item => item.ProductId == request.ProductId);
                    if (existingCartItem != null && !variantIdToUse.HasValue)
                    {
                        variantIdToUse = existingCartItem.VariantId;
                        _logger.LogDebug("UpdateCartItemQuantityCommand - Using variantId from existing cart item (by ProductId only): {VariantId}", variantIdToUse);
                    }
                }

                _logger.LogDebug("UpdateCartItemQuantityCommand - Fetching product - ProductId: {ProductId}", request.ProductId);
                
                var product = await _productRepository.GetByIdAsync(request.ProductId, cancellationToken);
                if (product is null)
                {
                    _logger.LogWarning("UpdateCartItemQuantityCommand - Product not found - ProductId: {ProductId}", request.ProductId);
                    return Result<CartDto>.Failure("محصول مورد نظر یافت نشد.");
                }

                decimal unitPrice;
                decimal? compareAtPrice;
                string? thumbnailPath = product.FeaturedImagePath;
                bool trackInventory;
                int stockQuantity;

                // If offerId is provided, use offer pricing and inventory
                if (request.OfferId.HasValue && request.OfferId.Value != Guid.Empty)
                {
                    var offer = await _productOfferRepository.GetByIdAsync(request.OfferId.Value, cancellationToken);
                    if (offer is null || offer.ProductId != request.ProductId || !offer.IsActive || !offer.IsPublished)
                    {
                        _logger.LogWarning("UpdateCartItemQuantityCommand - Offer not valid - OfferId: {OfferId}, ProductId: {ProductId}", request.OfferId, request.ProductId);
                        return Result<CartDto>.Failure("پیشنهاد انتخاب شده معتبر نیست.");
                    }

                    unitPrice = offer.Price ?? 0m;
                    compareAtPrice = offer.CompareAtPrice;
                    trackInventory = offer.TrackInventory;
                    stockQuantity = offer.StockQuantity;

                    if (unitPrice <= 0)
                    {
                        return Result<CartDto>.Failure("این پیشنهاد قیمت ندارد.");
                    }
                }
                // Use variant pricing and inventory if variant is selected
                else if (variantIdToUse.HasValue && variantIdToUse.Value != Guid.Empty)
                {
                    _logger.LogDebug("UpdateCartItemQuantityCommand - Using variant pricing - VariantId: {VariantId}", variantIdToUse);
                    
                    var variant = product.Variants.FirstOrDefault(v => v.Id == variantIdToUse.Value && v.IsActive);
                    if (variant is null)
                    {
                        _logger.LogWarning("UpdateCartItemQuantityCommand - Variant not found or inactive - VariantId: {VariantId}", variantIdToUse);
                        return Result<CartDto>.Failure("Variant انتخاب شده معتبر نیست.");
                    }

                    unitPrice = variant.Price ?? product.Price ?? 0m;
                    compareAtPrice = variant.CompareAtPrice ?? product.CompareAtPrice;
                    trackInventory = product.TrackInventory;
                    // Use variant stock if it has stock (> 0), otherwise use product stock
                    stockQuantity = variant.StockQuantity > 0 ? variant.StockQuantity : product.StockQuantity;
                    
                    if (!string.IsNullOrWhiteSpace(variant.ImagePath))
                    {
                        thumbnailPath = variant.ImagePath;
                    }

                    // Check final price
                    if (unitPrice <= 0)
                    {
                        _logger.LogWarning("UpdateCartItemQuantityCommand - Variant has no price - ProductId: {ProductId}, VariantId: {VariantId}", 
                            request.ProductId, variantIdToUse);
                        return Result<CartDto>.Failure("این محصول قیمت ندارد.");
                    }
                }
                else
                {
                    _logger.LogDebug("UpdateCartItemQuantityCommand - Using product pricing");
                    
                    if (product.IsCustomOrder)
                    {
                        _logger.LogWarning("UpdateCartItemQuantityCommand - Product is custom order - ProductId: {ProductId}", request.ProductId);
                        return Result<CartDto>.Failure("این محصول حالت سفارشی دارد و نمی‌توان آن را به سبد خرید اضافه کرد.");
                    }

                    if (!product.Price.HasValue || product.Price.Value <= 0)
                    {
                        _logger.LogWarning("UpdateCartItemQuantityCommand - Product has no price - ProductId: {ProductId}, Price: {Price}", 
                            request.ProductId, product.Price);
                        return Result<CartDto>.Failure("این محصول قیمت ندارد.");
                    }

                    unitPrice = product.Price.Value;
                    compareAtPrice = product.CompareAtPrice;
                    trackInventory = product.TrackInventory;
                    stockQuantity = product.StockQuantity;
                }

                // Check stock availability
                if (trackInventory)
                {
                    // Get current quantity in cart for this product/variant combination
                    var currentQuantityInCart = existingCartItem?.Quantity ?? 0;
                    var availableStock = stockQuantity + currentQuantityInCart; // Add back current cart quantity
                    
                    if (request.Quantity > availableStock)
                    {
                        _logger.LogWarning("UpdateCartItemQuantityCommand - Insufficient stock - ProductId: {ProductId}, Requested: {Quantity}, Available: {AvailableStock}", 
                            request.ProductId, request.Quantity, availableStock);
                        return Result<CartDto>.Failure($"فقط {availableStock} عدد از این محصول در انبار موجود است.");
                    }
                }
                
                _logger.LogDebug("UpdateCartItemQuantityCommand - Setting item quantity - ProductId: {ProductId}, Quantity: {Quantity}, UnitPrice: {UnitPrice}, VariantId: {VariantId}", 
                    request.ProductId, request.Quantity, unitPrice, variantIdToUse);
                
                cart.SetItemQuantity(
                    product.Id,
                    product.Name,
                    product.SeoSlug,
                    unitPrice,
                    compareAtPrice,
                    thumbnailPath,
                    product.Type,
                    request.Quantity,
                    variantIdToUse,
                    request.OfferId);

                var audit = _auditContext.Capture();
                // Only set UpdaterId and Ip here, UpdateDate will be set by AuditInterceptor
                cart.UpdaterId = audit.UserId;
                cart.Ip = audit.IpAddress;

                _logger.LogDebug("UpdateCartItemQuantityCommand - Updating cart in repository");
                
                await _cartRepository.UpdateAsync(cart, cancellationToken);

                _logger.LogInformation("UpdateCartItemQuantityCommand.Handle completed successfully - ProductId: {ProductId}, Quantity: {Quantity}", 
                    request.ProductId, request.Quantity);
                
                return Result<CartDto>.Success(cart.ToDto());
            }
            catch (DomainException ex)
            {
                _logger.LogWarning(ex, "UpdateCartItemQuantityCommand.Handle DomainException - ProductId: {ProductId}, Quantity: {Quantity}, VariantId: {VariantId}, Message: {Message}", 
                    request.ProductId, request.Quantity, request.VariantId, ex.Message);
                return Result<CartDto>.Failure(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UpdateCartItemQuantityCommand.Handle exception - ProductId: {ProductId}, Quantity: {Quantity}, VariantId: {VariantId}", 
                    request.ProductId, request.Quantity, request.VariantId);
                throw;
            }
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

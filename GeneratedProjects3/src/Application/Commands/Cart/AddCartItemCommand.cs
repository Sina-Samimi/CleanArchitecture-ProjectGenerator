using System;
using System.Threading;
using System.Threading.Tasks;
using LogTableRenameTest.Application.Abstractions.Messaging;
using LogTableRenameTest.Application.DTOs.Cart;
using LogTableRenameTest.Application.Interfaces;
using LogTableRenameTest.Domain.Constants;
using LogTableRenameTest.Domain.Entities.Catalog;
using LogTableRenameTest.Domain.Entities.Orders;
using LogTableRenameTest.SharedKernel.BaseTypes;

namespace LogTableRenameTest.Application.Commands.Cart;

public sealed record AddCartItemCommand(
    string? UserId,
    Guid? AnonymousId,
    Guid ProductId,
    int Quantity,
    Guid? OfferId = null,
    Guid? VariantId = null) : ICommand<CartDto>
{
    public sealed class Handler : ICommandHandler<AddCartItemCommand, CartDto>
    {
        private readonly IShoppingCartRepository _cartRepository;
        private readonly IProductRepository _productRepository;
        private readonly IProductOfferRepository _productOfferRepository;
        private readonly IAuditContext _auditContext;
        private readonly ISellerProfileRepository _sellerProfileRepository;

        public Handler(
            IShoppingCartRepository cartRepository,
            IProductRepository productRepository,
            IProductOfferRepository productOfferRepository,
            IAuditContext auditContext,
            ISellerProfileRepository sellerProfileRepository)
        {
            _cartRepository = cartRepository;
            _productRepository = productRepository;
            _productOfferRepository = productOfferRepository;
            _auditContext = auditContext;
            _sellerProfileRepository = sellerProfileRepository;
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

            // Check if product has variants and if variant is required
            // Only require variant if product has variant attributes AND active variants
            var hasActiveVariants = product.HasVariants && product.Variants.Any(v => v.IsActive);
            if (hasActiveVariants)
            {
                // Product has variants, variant selection is required
                if (!request.VariantId.HasValue || request.VariantId.Value == Guid.Empty)
                {
                    return Result<CartDto>.Failure("لطفاً گزینه‌های محصول را انتخاب کنید.");
                }

                var selectedVariant = product.Variants.FirstOrDefault(v => v.Id == request.VariantId.Value && v.IsActive);
                if (selectedVariant is null)
                {
                    return Result<CartDto>.Failure("گزینه انتخاب شده معتبر نیست.");
                }
            }
            // If product doesn't have variants, proceed without variantId (variantId will be null)

            decimal unitPrice;
            decimal? compareAtPrice;
            bool trackInventory;
            int stockQuantity;

            // If offerId is provided, use offer pricing and inventory
            if (request.OfferId.HasValue && request.OfferId.Value != Guid.Empty)
            {
                var offer = await _productOfferRepository.GetByIdAsync(request.OfferId.Value, cancellationToken);
                if (offer is null || offer.IsDeleted || !offer.IsActive || !offer.IsPublished)
                {
                    return Result<CartDto>.Failure("پیشنهاد انتخاب شده در دسترس نیست.");
                }

                if (offer.ProductId != request.ProductId)
                {
                    return Result<CartDto>.Failure("پیشنهاد انتخاب شده مربوط به این محصول نیست.");
                }

                // Check if seller is active
                if (!string.IsNullOrWhiteSpace(offer.SellerId))
                {
                    var seller = await _sellerProfileRepository.GetByUserIdAsync(offer.SellerId, cancellationToken);
                    if (seller is null || seller.IsDeleted || !seller.IsActive)
                    {
                        return Result<CartDto>.Failure("فروشنده این محصول غیرفعال است و امکان ثبت سفارش وجود ندارد.");
                    }
                }

                if (!offer.Price.HasValue || offer.Price.Value <= 0)
                {
                    return Result<CartDto>.Failure("این پیشنهاد قیمت ندارد.");
                }

                if (offer.TrackInventory && offer.StockQuantity <= 0)
                {
                    return Result<CartDto>.Failure("این پیشنهاد موجودی کافی ندارد.");
                }

                unitPrice = offer.Price.Value;
                compareAtPrice = offer.CompareAtPrice;
                trackInventory = offer.TrackInventory;
                stockQuantity = offer.StockQuantity;
            }
            else
            {
                // Use variant pricing and inventory if variant is selected
                if (request.VariantId.HasValue && request.VariantId.Value != Guid.Empty)
                {
                    var variant = product.Variants.FirstOrDefault(v => v.Id == request.VariantId.Value && v.IsActive);
                    if (variant is null)
                    {
                        return Result<CartDto>.Failure("گزینه انتخاب شده معتبر نیست.");
                    }

                    // Check if product is custom order
                    if (product.IsCustomOrder)
                    {
                        return Result<CartDto>.Failure("این محصول حالت سفارشی دارد و نمی‌توان آن را به سبد خرید اضافه کرد.");
                    }

                    // Check if the main product has a seller and if that seller is active
                    if (!string.IsNullOrWhiteSpace(product.SellerId))
                    {
                        var mainProductSeller = await _sellerProfileRepository.GetByUserIdAsync(product.SellerId, cancellationToken);
                        if (mainProductSeller is null || mainProductSeller.IsDeleted || !mainProductSeller.IsActive)
                        {
                            return Result<CartDto>.Failure("فروشنده این محصول غیرفعال است و امکان ثبت سفارش وجود ندارد.");
                        }
                    }

                    // Use variant price if available, otherwise use product price
                    unitPrice = variant.Price ?? product.Price ?? 0m;
                    compareAtPrice = variant.CompareAtPrice ?? product.CompareAtPrice;
                    trackInventory = product.TrackInventory;
                    // Use variant stock if it has stock (> 0), otherwise use product stock
                    // If variant stock is 0, it means variant doesn't have its own stock, so use product stock
                    stockQuantity = variant.StockQuantity > 0 ? variant.StockQuantity : product.StockQuantity;

                    // Check final price (from variant or product)
                    if (unitPrice <= 0)
                    {
                        return Result<CartDto>.Failure("این محصول قیمت ندارد.");
                    }

                    // Check product stock if inventory tracking is enabled
                    // Consider existing quantity in cart for this product/variant
                    var existingCartItemForVariant = cart.Items.FirstOrDefault(item => 
                        item.ProductId == request.ProductId && item.VariantId == request.VariantId);
                    var currentQuantityInCart = existingCartItemForVariant?.Quantity ?? 0;
                    var availableStock = stockQuantity + currentQuantityInCart; // Add back current cart quantity
                    
                    if (trackInventory && availableStock < request.Quantity)
                    {
                        return Result<CartDto>.Failure($"فقط {availableStock} عدد از این محصول در انبار موجود است.");
                    }
                }
                else
                {
                    // Use product pricing and inventory
                    if (product.IsCustomOrder)
                    {
                        return Result<CartDto>.Failure("این محصول حالت سفارشی دارد و نمی‌توان آن را به سبد خرید اضافه کرد.");
                    }

                    // Check if the main product has a seller and if that seller is active
                    if (!string.IsNullOrWhiteSpace(product.SellerId))
                    {
                        var mainProductSeller = await _sellerProfileRepository.GetByUserIdAsync(product.SellerId, cancellationToken);
                        if (mainProductSeller is null || mainProductSeller.IsDeleted || !mainProductSeller.IsActive)
                        {
                            return Result<CartDto>.Failure("فروشنده این محصول غیرفعال است و امکان ثبت سفارش وجود ندارد.");
                        }
                    }

                    if (!product.Price.HasValue || product.Price.Value <= 0)
                    {
                        return Result<CartDto>.Failure("این محصول قیمت ندارد.");
                    }

                    // Check product stock if inventory tracking is enabled
                    // Consider existing quantity in cart for this product
                    var existingCartItemForProduct = cart.Items.FirstOrDefault(item => 
                        item.ProductId == request.ProductId && item.VariantId == null);
                    var currentQuantityInCart = existingCartItemForProduct?.Quantity ?? 0;
                    var availableStock = product.StockQuantity + currentQuantityInCart; // Add back current cart quantity
                    
                    if (product.TrackInventory && availableStock < request.Quantity)
                    {
                        return Result<CartDto>.Failure($"فقط {availableStock} عدد از این محصول در انبار موجود است.");
                    }

                    unitPrice = product.Price.Value;
                    compareAtPrice = product.CompareAtPrice;
                    trackInventory = product.TrackInventory;
                    stockQuantity = product.StockQuantity;
                }
            }

            // Use variant image if available
            string? thumbnailPath = product.FeaturedImagePath;
            if (request.VariantId.HasValue && request.VariantId.Value != Guid.Empty)
            {
                var variant = product.Variants.FirstOrDefault(v => v.Id == request.VariantId.Value);
                if (variant is not null && !string.IsNullOrWhiteSpace(variant.ImagePath))
                {
                    thumbnailPath = variant.ImagePath;
                }
            }

            var item = cart.AddItem(
                product.Id,
                product.Name,
                product.SeoSlug,
                unitPrice,
                compareAtPrice,
                thumbnailPath,
                product.Type,
                request.Quantity,
                request.VariantId,
                request.OfferId);

            var audit = _auditContext.Capture();

            if (cartWasCreated || cart.CreatorId == SystemUsers.AutomationId)
            {
                cart.CreatorId = audit.UserId;
                cart.CreateDate = audit.Timestamp;
            }

            // Only set UpdaterId and Ip here, UpdateDate will be set by AuditInterceptor
            cart.UpdaterId = audit.UserId;
            cart.Ip = audit.IpAddress;

            if (item.CreatorId == SystemUsers.AutomationId || cartWasCreated)
            {
                item.CreatorId = audit.UserId;
                item.CreateDate = audit.Timestamp;
            }

            // Only set UpdaterId and Ip here, UpdateDate will be set by AuditInterceptor
            item.UpdaterId = audit.UserId;
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

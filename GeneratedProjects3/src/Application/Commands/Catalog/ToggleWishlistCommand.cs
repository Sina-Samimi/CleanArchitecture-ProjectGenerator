using System;
using System.Threading;
using System.Threading.Tasks;
using LogTableRenameTest.Application.Abstractions.Messaging;
using LogTableRenameTest.Application.Interfaces;
using LogTableRenameTest.Domain.Constants;
using LogTableRenameTest.Domain.Entities.Catalog;
using LogTableRenameTest.SharedKernel.BaseTypes;

namespace LogTableRenameTest.Application.Commands.Catalog;

public sealed record ToggleWishlistCommand(
    string UserId,
    Guid ProductId) : ICommand<bool>
{
    public sealed class Handler : ICommandHandler<ToggleWishlistCommand, bool>
    {
        private readonly IWishlistRepository _wishlistRepository;
        private readonly IProductRepository _productRepository;
        private readonly IAuditContext _auditContext;

        public Handler(
            IWishlistRepository wishlistRepository,
            IProductRepository productRepository,
            IAuditContext auditContext)
        {
            _wishlistRepository = wishlistRepository;
            _productRepository = productRepository;
            _auditContext = auditContext;
        }

        public async Task<Result<bool>> Handle(ToggleWishlistCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.UserId))
            {
                return Result<bool>.Failure("کاربر باید وارد سیستم شده باشد.");
            }

            if (request.ProductId == Guid.Empty)
            {
                return Result<bool>.Failure("محصول انتخاب شده معتبر نیست.");
            }

            var normalizedUserId = request.UserId.Trim();

            // Check if product exists and is published
            var product = await _productRepository.GetByIdAsync(request.ProductId, cancellationToken);
            if (product is null || product.IsDeleted || !product.IsPublished)
            {
                return Result<bool>.Failure("محصول مورد نظر در دسترس نیست.");
            }

            // Check if already in wishlist
            var existingWishlist = await _wishlistRepository.GetByUserAndProductAsync(normalizedUserId, request.ProductId, cancellationToken);

            if (existingWishlist is not null)
            {
                // Product already in wishlist, return error message
                return Result<bool>.Failure("این محصول قبلا به لیست علاقه‌مندی‌های شما اضافه شده است.");
            }

            // Add to wishlist
            var wishlist = new Wishlist(normalizedUserId, request.ProductId);
            var audit = _auditContext.Capture();
            wishlist.CreatorId = audit.UserId;
            wishlist.UpdaterId = audit.UserId;
            wishlist.Ip = audit.IpAddress;

            await _wishlistRepository.AddAsync(wishlist, cancellationToken);
            return Result<bool>.Success(true); // true means added
        }
    }
}


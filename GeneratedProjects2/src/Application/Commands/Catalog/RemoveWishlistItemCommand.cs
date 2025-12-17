using System;
using System.Threading;
using System.Threading.Tasks;
using LogsDtoCloneTest.Application.Abstractions.Messaging;
using LogsDtoCloneTest.Application.Interfaces;
using LogsDtoCloneTest.SharedKernel.BaseTypes;

namespace LogsDtoCloneTest.Application.Commands.Catalog;

public sealed record RemoveWishlistItemCommand(
    string UserId,
    Guid ProductId) : ICommand<bool>
{
    public sealed class Handler : ICommandHandler<RemoveWishlistItemCommand, bool>
    {
        private readonly IWishlistRepository _wishlistRepository;

        public Handler(IWishlistRepository wishlistRepository)
        {
            _wishlistRepository = wishlistRepository;
        }

        public async Task<Result<bool>> Handle(RemoveWishlistItemCommand request, CancellationToken cancellationToken)
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

            // Check if product is in wishlist
            var existingWishlist = await _wishlistRepository.GetByUserAndProductAsync(normalizedUserId, request.ProductId, cancellationToken);

            if (existingWishlist is null)
            {
                return Result<bool>.Failure("این محصول در لیست علاقه‌مندی‌های شما وجود ندارد.");
            }

            // Remove from wishlist
            await _wishlistRepository.RemoveAsync(existingWishlist, cancellationToken);
            return Result<bool>.Success(true);
        }
    }
}


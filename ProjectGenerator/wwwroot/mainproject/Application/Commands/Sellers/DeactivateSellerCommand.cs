using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MobiRooz.Application.Abstractions.Messaging;
using MobiRooz.Application.Interfaces;
using MobiRooz.SharedKernel.BaseTypes;
using Microsoft.AspNetCore.Identity;

namespace MobiRooz.Application.Commands.Sellers;

public sealed record DeactivateSellerCommand(Guid SellerId, string? Reason = null) : ICommand
{
    public sealed class Handler : ICommandHandler<DeactivateSellerCommand>
    {
        private const string DefaultReason = "حساب فروشنده به صورت موقت غیرفعال شده است. لطفاً با پشتیبانی تماس بگیرید.";

        private readonly ISellerProfileRepository _sellerRepository;
        private readonly IAuditContext _auditContext;
        private readonly UserManager<Domain.Entities.ApplicationUser> _userManager;
        private readonly IProductOfferRepository _productOfferRepository;

        public Handler(
            ISellerProfileRepository sellerRepository,
            IAuditContext auditContext,
            UserManager<Domain.Entities.ApplicationUser> userManager,
            IProductOfferRepository productOfferRepository)
        {
            _sellerRepository = sellerRepository;
            _auditContext = auditContext;
            _userManager = userManager;
            _productOfferRepository = productOfferRepository;
        }

        public async Task<Result> Handle(DeactivateSellerCommand request, CancellationToken cancellationToken)
        {
            var seller = await _sellerRepository.GetByIdForUpdateAsync(request.SellerId, cancellationToken);
            if (seller is null || seller.IsDeleted)
            {
                return Result.Failure("پروفایل فروشنده مورد نظر یافت نشد.");
            }

            seller.SetActive(false);

            var audit = _auditContext.Capture();
            seller.UpdaterId = audit.UserId;
            seller.UpdateDate = audit.Timestamp;
            seller.Ip = audit.IpAddress;

            if (!string.IsNullOrWhiteSpace(seller.UserId))
            {
                var user = await _userManager.FindByIdAsync(seller.UserId);
                if (user is not null && !user.IsDeleted)
                {
                    user.IsActive = false;
                    user.DeactivatedOn = DateTimeOffset.UtcNow;
                    user.DeactivationReason = string.IsNullOrWhiteSpace(request.Reason)
                        ? DefaultReason
                        : request.Reason!.Trim();
                    user.LastModifiedOn = DateTimeOffset.UtcNow;

                    var updateResult = await _userManager.UpdateAsync(user);
                    if (!updateResult.Succeeded)
                    {
                        return Result.Failure(string.Join("; ", updateResult.Errors.Select(error => error.Description)));
                    }

                    var stampResult = await _userManager.UpdateSecurityStampAsync(user);
                    if (!stampResult.Succeeded)
                    {
                        return Result.Failure(string.Join("; ", stampResult.Errors.Select(error => error.Description)));
                    }
                }
            }

            await _sellerRepository.UpdateAsync(seller, cancellationToken);

            // Set stock quantity to 0 for all product offers of this seller
            if (!string.IsNullOrWhiteSpace(seller.UserId))
            {
                var offers = await _productOfferRepository.GetBySellerIdForUpdateAsync(
                    seller.UserId, 
                    includeInactive: true, 
                    cancellationToken);

                var offersToUpdate = offers
                    .Where(o => !o.IsDeleted && o.TrackInventory && o.StockQuantity > 0)
                    .ToList();

                foreach (var offer in offersToUpdate)
                {
                    offer.UpdateInventory(offer.TrackInventory, 0);
                }

                // Save all changes at once if there are any offers to update
                if (offersToUpdate.Any())
                {
                    await _productOfferRepository.UpdateBatchAsync(offersToUpdate, cancellationToken);
                }
            }

            return Result.Success();
        }
    }
}

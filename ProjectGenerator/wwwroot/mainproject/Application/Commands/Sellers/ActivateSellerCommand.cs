using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MobiRooz.Application.Abstractions.Messaging;
using MobiRooz.Application.Interfaces;
using MobiRooz.SharedKernel.BaseTypes;
using Microsoft.AspNetCore.Identity;

namespace MobiRooz.Application.Commands.Sellers;

public sealed record ActivateSellerCommand(Guid SellerId) : ICommand
{
    public sealed class Handler : ICommandHandler<ActivateSellerCommand>
    {
        private readonly ISellerProfileRepository _sellerRepository;
        private readonly IAuditContext _auditContext;
        private readonly UserManager<Domain.Entities.ApplicationUser> _userManager;

        public Handler(
            ISellerProfileRepository sellerRepository,
            IAuditContext auditContext,
            UserManager<Domain.Entities.ApplicationUser> userManager)
        {
            _sellerRepository = sellerRepository;
            _auditContext = auditContext;
            _userManager = userManager;
        }

        public async Task<Result> Handle(ActivateSellerCommand request, CancellationToken cancellationToken)
        {
            var seller = await _sellerRepository.GetByIdForUpdateAsync(request.SellerId, cancellationToken);
            if (seller is null || seller.IsDeleted)
            {
                return Result.Failure("پروفایل فروشنده مورد نظر یافت نشد.");
            }

            seller.SetActive(true);

            var audit = _auditContext.Capture();
            seller.UpdaterId = audit.UserId;
            seller.UpdateDate = audit.Timestamp;
            seller.Ip = audit.IpAddress;

            if (!string.IsNullOrWhiteSpace(seller.UserId))
            {
                var user = await _userManager.FindByIdAsync(seller.UserId);
                if (user is not null && !user.IsDeleted)
                {
                    user.IsActive = true;
                    user.DeactivatedOn = null;
                    user.DeactivationReason = null;
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

            return Result.Success();
        }
    }
}

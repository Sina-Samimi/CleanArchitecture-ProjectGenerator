using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LogsDtoCloneTest.Application.Abstractions.Messaging;
using LogsDtoCloneTest.Application.Interfaces;
using LogsDtoCloneTest.SharedKernel.Authorization;
using LogsDtoCloneTest.SharedKernel.BaseTypes;
using Microsoft.AspNetCore.Identity;

namespace LogsDtoCloneTest.Application.Commands.Sellers;

public sealed record RemoveSellerProfileCommand(Guid SellerId) : ICommand
{
    public sealed class Handler : ICommandHandler<RemoveSellerProfileCommand>
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

        public async Task<Result> Handle(RemoveSellerProfileCommand request, CancellationToken cancellationToken)
        {
            var seller = await _sellerRepository.GetByIdForUpdateAsync(request.SellerId, cancellationToken);
            if (seller is null || seller.IsDeleted)
            {
                return Result.Failure("پروفایل فروشنده مورد نظر یافت نشد.");
            }

            var audit = _auditContext.Capture();

            if (!string.IsNullOrWhiteSpace(seller.UserId))
            {
                var user = await _userManager.FindByIdAsync(seller.UserId);
                if (user is not null && !user.IsDeleted)
                {
                    var removeRoleResult = await _userManager.RemoveFromRoleAsync(user, RoleNames.Seller);
                    if (!removeRoleResult.Succeeded)
                    {
                        return Result.Failure(string.Join("; ", removeRoleResult.Errors.Select(error => error.Description)));
                    }

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

            seller.SetActive(false);
            seller.ConnectToUser(null);
            seller.IsDeleted = true;
            seller.RemoveDate = audit.Timestamp;
            seller.UpdaterId = audit.UserId;
            seller.UpdateDate = audit.Timestamp;
            seller.Ip = audit.IpAddress;

            await _sellerRepository.UpdateAsync(seller, cancellationToken);

            return Result.Success();
        }
    }
}

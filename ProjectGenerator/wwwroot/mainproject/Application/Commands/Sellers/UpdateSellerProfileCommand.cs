using System;
using System.Threading;
using System.Threading.Tasks;
using Attar.Application.Abstractions.Messaging;
using Attar.Application.Interfaces;
using Attar.SharedKernel.BaseTypes;

namespace Attar.Application.Commands.Sellers;

public sealed record UpdateSellerProfileCommand(
    Guid Id,
    string DisplayName,
    string? LicenseNumber,
    DateOnly? LicenseIssueDate,
    DateOnly? LicenseExpiryDate,
    string? ShopAddress,
    string? WorkingHours,
    int? ExperienceYears,
    string? Bio,
    string? AvatarUrl,
    string? ContactEmail,
    string? ContactPhone,
    string? UserId,
    bool IsActive,
    decimal? SellerSharePercentage = null) : ICommand
{
    public sealed class Handler : ICommandHandler<UpdateSellerProfileCommand>
    {
        private readonly ISellerProfileRepository _sellerRepository;
        private readonly IAuditContext _auditContext;

        public Handler(ISellerProfileRepository sellerRepository, IAuditContext auditContext)
        {
            _sellerRepository = sellerRepository;
            _auditContext = auditContext;
        }

        public async Task<Result> Handle(UpdateSellerProfileCommand request, CancellationToken cancellationToken)
        {
            var seller = await _sellerRepository.GetByIdForUpdateAsync(request.Id, cancellationToken);
            if (seller is null || seller.IsDeleted)
            {
                return Result.Failure("پروفایل فروشنده مورد نظر یافت نشد.");
            }

            if (string.IsNullOrWhiteSpace(request.DisplayName))
            {
                return Result.Failure("نام فروشنده را وارد کنید.");
            }

            var userId = string.IsNullOrWhiteSpace(request.UserId)
                ? null
                : request.UserId.Trim();

            if (!string.IsNullOrWhiteSpace(userId))
            {
                var exists = await _sellerRepository.ExistsByUserIdAsync(userId, request.Id, cancellationToken);
                if (exists)
                {
                    return Result.Failure("این کاربر در حال حاضر به پروفایل فروشنده دیگری متصل است.");
                }
            }

            seller.UpdateDisplayName(request.DisplayName);
            seller.UpdateBusinessInfo(
                request.LicenseNumber,
                request.LicenseIssueDate,
                request.LicenseExpiryDate,
                request.ShopAddress,
                request.WorkingHours,
                request.ExperienceYears,
                request.Bio);
            seller.UpdateMedia(request.AvatarUrl);
            seller.UpdateContact(request.ContactEmail, request.ContactPhone);
            seller.ConnectToUser(userId);
            seller.SetSellerSharePercentage(request.SellerSharePercentage);
            seller.SetActive(request.IsActive);

            var audit = _auditContext.Capture();
            seller.UpdaterId = audit.UserId;
            seller.UpdateDate = audit.Timestamp;
            seller.Ip = audit.IpAddress;

            await _sellerRepository.UpdateAsync(seller, cancellationToken);

            return Result.Success();
        }
    }
}

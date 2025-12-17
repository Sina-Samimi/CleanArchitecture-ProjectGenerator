using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Attar.Application.Abstractions.Messaging;
using Attar.Application.DTOs.Sellers;
using Attar.Application.Interfaces;
using Attar.SharedKernel.BaseTypes;

namespace Attar.Application.Queries.Sellers;

public sealed record GetSellerProfileByUserIdQuery(string UserId) : IQuery<SellerProfileDetailDto>
{
    public sealed class Handler : IQueryHandler<GetSellerProfileByUserIdQuery, SellerProfileDetailDto>
    {
        private readonly ISellerProfileRepository _sellerRepository;

        public Handler(ISellerProfileRepository sellerRepository)
        {
            _sellerRepository = sellerRepository;
        }

        public async Task<Result<SellerProfileDetailDto>> Handle(GetSellerProfileByUserIdQuery request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.UserId))
            {
                return Result<SellerProfileDetailDto>.Failure("شناسه کاربری معتبر نیست.");
            }

            var seller = await _sellerRepository.GetByUserIdAsync(request.UserId.Trim(), cancellationToken);
            if (seller is null || seller.IsDeleted)
            {
                return Result<SellerProfileDetailDto>.Failure("پروفایل فروشنده یافت نشد.");
            }

            var dto = new SellerProfileDetailDto(
                seller.Id,
                seller.DisplayName,
                seller.LicenseNumber,
                seller.LicenseIssueDate,
                seller.LicenseExpiryDate,
                seller.ShopAddress,
                seller.WorkingHours,
                seller.ExperienceYears,
                seller.Bio,
                seller.AvatarUrl,
                seller.ContactEmail,
                seller.ContactPhone,
                seller.UserId,
                seller.IsActive,
                seller.SellerSharePercentage,
                seller.CreateDate,
                seller.UpdateDate);

            return Result<SellerProfileDetailDto>.Success(dto);
        }
    }
}


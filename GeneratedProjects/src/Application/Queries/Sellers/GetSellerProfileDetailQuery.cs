using System;
using System.Threading;
using System.Threading.Tasks;
using TestAttarClone.Application.Abstractions.Messaging;
using TestAttarClone.Application.DTOs.Sellers;
using TestAttarClone.Application.Interfaces;
using TestAttarClone.SharedKernel.BaseTypes;

namespace TestAttarClone.Application.Queries.Sellers;

public sealed record GetSellerProfileDetailQuery(Guid Id) : IQuery<SellerProfileDetailDto>
{
    public sealed class Handler : IQueryHandler<GetSellerProfileDetailQuery, SellerProfileDetailDto>
    {
        private readonly ISellerProfileRepository _sellerRepository;

        public Handler(ISellerProfileRepository sellerRepository)
        {
            _sellerRepository = sellerRepository;
        }

        public async Task<Result<SellerProfileDetailDto>> Handle(GetSellerProfileDetailQuery request, CancellationToken cancellationToken)
        {
            var seller = await _sellerRepository.GetByIdAsync(request.Id, cancellationToken);
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

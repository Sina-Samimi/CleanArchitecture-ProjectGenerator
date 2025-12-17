using System;
using System.Threading;
using System.Threading.Tasks;
using LogsDtoCloneTest.Application.Abstractions.Messaging;
using LogsDtoCloneTest.Application.DTOs.Sellers;
using LogsDtoCloneTest.Application.Interfaces;
using LogsDtoCloneTest.SharedKernel.BaseTypes;

namespace LogsDtoCloneTest.Application.Queries.Sellers;

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

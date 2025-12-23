using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MobiRooz.Application.Abstractions.Messaging;
using MobiRooz.Application.DTOs.Sellers;
using MobiRooz.Application.Interfaces;
using MobiRooz.SharedKernel.BaseTypes;

namespace MobiRooz.Application.Queries.Sellers;

public sealed record GetSellerProfilesQuery : IQuery<SellerProfileListResultDto>
{
    public sealed class Handler : IQueryHandler<GetSellerProfilesQuery, SellerProfileListResultDto>
    {
        private readonly ISellerProfileRepository _sellerRepository;

        public Handler(ISellerProfileRepository sellerRepository)
        {
            _sellerRepository = sellerRepository;
        }

        public async Task<Result<SellerProfileListResultDto>> Handle(GetSellerProfilesQuery request, CancellationToken cancellationToken)
        {
            var sellers = await _sellerRepository.GetAllAsync(cancellationToken);

            var items = sellers
                .Where(seller => !seller.IsDeleted)
                .OrderByDescending(seller => seller.UpdateDate)
                .Select(seller => new SellerProfileListItemDto(
                    seller.Id,
                    seller.DisplayName,
                    seller.LicenseNumber,
                    seller.LicenseIssueDate,
                    seller.LicenseExpiryDate,
                    seller.ShopAddress,
                    seller.WorkingHours,
                    seller.ExperienceYears,
                    seller.Bio,
                    seller.ContactEmail,
                    seller.ContactPhone,
                    seller.UserId,
                    seller.IsActive,
                    seller.SellerSharePercentage,
                    seller.CreateDate,
                    seller.UpdateDate))
                .ToArray();

            var activeCount = items.Count(item => item.IsActive);
            var inactiveCount = items.Length - activeCount;

            return Result<SellerProfileListResultDto>.Success(new SellerProfileListResultDto(items, activeCount, inactiveCount));
        }
    }
}

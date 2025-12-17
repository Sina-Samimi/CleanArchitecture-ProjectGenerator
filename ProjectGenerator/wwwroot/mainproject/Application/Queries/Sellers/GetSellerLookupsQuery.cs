using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Attar.Application.Abstractions.Messaging;
using Attar.Application.DTOs.Sellers;
using Attar.Application.Interfaces;
using Attar.SharedKernel.BaseTypes;

namespace Attar.Application.Queries.Sellers;

public sealed record GetSellerLookupsQuery : IQuery<IReadOnlyCollection<SellerLookupDto>>
{
    public sealed class Handler : IQueryHandler<GetSellerLookupsQuery, IReadOnlyCollection<SellerLookupDto>>
    {
        private readonly ISellerProfileRepository _sellerRepository;

        public Handler(ISellerProfileRepository sellerRepository)
        {
            _sellerRepository = sellerRepository;
        }

        public async Task<Result<IReadOnlyCollection<SellerLookupDto>>> Handle(GetSellerLookupsQuery request, CancellationToken cancellationToken)
        {
            var sellers = await _sellerRepository.GetAllAsync(cancellationToken);

            var lookups = sellers
                .Where(seller => !string.IsNullOrWhiteSpace(seller.UserId))
                .OrderByDescending(seller => seller.IsActive)
                .ThenBy(seller => seller.DisplayName)
                .Select(seller => new SellerLookupDto(
                    seller.Id,
                    seller.DisplayName,
                    seller.LicenseNumber,
                    seller.UserId,
                    seller.IsActive))
                .ToArray();

            return Result<IReadOnlyCollection<SellerLookupDto>>.Success(lookups);
        }
    }
}

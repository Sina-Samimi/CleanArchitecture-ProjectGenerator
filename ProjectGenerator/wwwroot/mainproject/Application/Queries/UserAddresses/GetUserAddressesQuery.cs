using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MobiRooz.Application.Abstractions.Messaging;
using MobiRooz.Application.DTOs.UserAddresses;
using MobiRooz.Application.Interfaces;
using MobiRooz.SharedKernel.BaseTypes;

namespace MobiRooz.Application.Queries.UserAddresses;

public sealed record GetUserAddressesQuery(string UserId) : IQuery<IReadOnlyCollection<UserAddressDto>>
{
    public sealed class Handler : IQueryHandler<GetUserAddressesQuery, IReadOnlyCollection<UserAddressDto>>
    {
        private readonly IUserAddressRepository _addressRepository;

        public Handler(IUserAddressRepository addressRepository)
        {
            _addressRepository = addressRepository;
        }

        public async Task<Result<IReadOnlyCollection<UserAddressDto>>> Handle(GetUserAddressesQuery request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.UserId))
            {
                return Result<IReadOnlyCollection<UserAddressDto>>.Failure("شناسه کاربر معتبر نیست.");
            }

            var addresses = await _addressRepository.GetByUserIdAsync(request.UserId, cancellationToken);

            var dtos = addresses.Select(address => new UserAddressDto(
                address.Id,
                address.Title,
                address.RecipientName,
                address.RecipientPhone,
                address.Province,
                address.City,
                address.PostalCode,
                address.AddressLine,
                address.Plaque,
                address.Unit,
                address.IsDefault)).ToList();

            return Result<IReadOnlyCollection<UserAddressDto>>.Success(dtos);
        }
    }
}

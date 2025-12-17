using System;
using System.Threading;
using System.Threading.Tasks;
using LogsDtoCloneTest.Application.Abstractions.Messaging;
using LogsDtoCloneTest.Application.DTOs.UserAddresses;
using LogsDtoCloneTest.Application.Interfaces;
using LogsDtoCloneTest.SharedKernel.BaseTypes;

namespace LogsDtoCloneTest.Application.Queries.UserAddresses;

public sealed record GetUserAddressQuery(Guid AddressId, string UserId) : IQuery<UserAddressDto>
{
    public sealed class Handler : IQueryHandler<GetUserAddressQuery, UserAddressDto>
    {
        private readonly IUserAddressRepository _addressRepository;

        public Handler(IUserAddressRepository addressRepository)
        {
            _addressRepository = addressRepository;
        }

        public async Task<Result<UserAddressDto>> Handle(GetUserAddressQuery request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.UserId))
            {
                return Result<UserAddressDto>.Failure("شناسه کاربر معتبر نیست.");
            }

            var address = await _addressRepository.GetByIdForUserAsync(request.AddressId, request.UserId, cancellationToken);
            if (address is null)
            {
                return Result<UserAddressDto>.Failure("آدرس مورد نظر یافت نشد.");
            }

            var dto = new UserAddressDto(
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
                address.IsDefault);

            return Result<UserAddressDto>.Success(dto);
        }
    }
}

using System;
using System.Threading;
using System.Threading.Tasks;
using Attar.Application.Abstractions.Messaging;
using Attar.Application.Interfaces;
using Attar.Domain.Entities;
using Attar.SharedKernel.BaseTypes;

namespace Attar.Application.Commands.UserAddresses;

public sealed record CreateUserAddressCommand(
    string UserId,
    string Title,
    string RecipientName,
    string RecipientPhone,
    string Province,
    string City,
    string PostalCode,
    string AddressLine,
    string? Plaque = null,
    string? Unit = null,
    bool IsDefault = false) : ICommand<Guid>
{
    public sealed class Handler : ICommandHandler<CreateUserAddressCommand, Guid>
    {
        private readonly IUserAddressRepository _addressRepository;
        private readonly IAuditContext _auditContext;

        public Handler(
            IUserAddressRepository addressRepository,
            IAuditContext auditContext)
        {
            _addressRepository = addressRepository;
            _auditContext = auditContext;
        }

        public async Task<Result<Guid>> Handle(CreateUserAddressCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.UserId))
            {
                return Result<Guid>.Failure("شناسه کاربر معتبر نیست.");
            }

            try
            {
                // If this is set as default, remove default from other addresses
                if (request.IsDefault)
                {
                    var currentDefault = await _addressRepository.GetDefaultByUserIdAsync(request.UserId, cancellationToken);
                    if (currentDefault is not null)
                    {
                        currentDefault.RemoveDefault();
                        await _addressRepository.UpdateAsync(currentDefault, cancellationToken);
                    }
                }

                var address = new UserAddress(
                    request.UserId,
                    request.Title,
                    request.RecipientName,
                    request.RecipientPhone,
                    request.Province,
                    request.City,
                    request.PostalCode,
                    request.AddressLine,
                    request.Plaque,
                    request.Unit,
                    request.IsDefault);

                var audit = _auditContext.Capture();
                address.CreatorId = audit.UserId;
                address.CreateDate = audit.Timestamp;
                address.UpdateDate = audit.Timestamp;
                address.Ip = audit.IpAddress;

                await _addressRepository.AddAsync(address, cancellationToken);

                return Result<Guid>.Success(address.Id);
            }
            catch (Domain.Exceptions.DomainException ex)
            {
                return Result<Guid>.Failure(ex.Message);
            }
        }
    }
}

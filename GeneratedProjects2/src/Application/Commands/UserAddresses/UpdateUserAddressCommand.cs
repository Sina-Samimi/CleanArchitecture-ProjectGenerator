using System;
using System.Threading;
using System.Threading.Tasks;
using LogsDtoCloneTest.Application.Abstractions.Messaging;
using LogsDtoCloneTest.Application.Interfaces;
using LogsDtoCloneTest.SharedKernel.BaseTypes;

namespace LogsDtoCloneTest.Application.Commands.UserAddresses;

public sealed record UpdateUserAddressCommand(
    Guid AddressId,
    string UserId,
    string Title,
    string RecipientName,
    string RecipientPhone,
    string Province,
    string City,
    string PostalCode,
    string AddressLine,
    string? Plaque = null,
    string? Unit = null) : ICommand
{
    public sealed class Handler : ICommandHandler<UpdateUserAddressCommand>
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

        public async Task<Result> Handle(UpdateUserAddressCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.UserId))
            {
                return Result.Failure("شناسه کاربر معتبر نیست.");
            }

            var address = await _addressRepository.GetByIdForUserAsync(request.AddressId, request.UserId, cancellationToken);
            if (address is null)
            {
                return Result.Failure("آدرس مورد نظر یافت نشد.");
            }

            try
            {
                address.Update(
                    request.Title,
                    request.RecipientName,
                    request.RecipientPhone,
                    request.Province,
                    request.City,
                    request.PostalCode,
                    request.AddressLine,
                    request.Plaque,
                    request.Unit);

                var audit = _auditContext.Capture();
                address.UpdaterId = audit.UserId;
                address.UpdateDate = audit.Timestamp;
                address.Ip = audit.IpAddress;

                await _addressRepository.UpdateAsync(address, cancellationToken);

                return Result.Success();
            }
            catch (Domain.Exceptions.DomainException ex)
            {
                return Result.Failure(ex.Message);
            }
        }
    }
}

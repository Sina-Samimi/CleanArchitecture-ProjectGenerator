using System;
using System.Threading;
using System.Threading.Tasks;
using Attar.Application.Abstractions.Messaging;
using Attar.Application.Interfaces;
using Attar.SharedKernel.BaseTypes;

namespace Attar.Application.Commands.UserAddresses;

public sealed record SetDefaultUserAddressCommand(
    Guid AddressId,
    string UserId) : ICommand
{
    public sealed class Handler : ICommandHandler<SetDefaultUserAddressCommand>
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

        public async Task<Result> Handle(SetDefaultUserAddressCommand request, CancellationToken cancellationToken)
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

            // Remove default from other addresses
            var currentDefault = await _addressRepository.GetDefaultByUserIdAsync(request.UserId, cancellationToken);
            if (currentDefault is not null && currentDefault.Id != request.AddressId)
            {
                currentDefault.RemoveDefault();
                await _addressRepository.UpdateAsync(currentDefault, cancellationToken);
            }

            address.SetAsDefault();

            var audit = _auditContext.Capture();
            address.UpdaterId = audit.UserId;
            address.UpdateDate = audit.Timestamp;
            address.Ip = audit.IpAddress;

            await _addressRepository.UpdateAsync(address, cancellationToken);

            return Result.Success();
        }
    }
}

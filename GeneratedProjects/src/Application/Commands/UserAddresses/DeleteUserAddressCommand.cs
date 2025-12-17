using System;
using System.Threading;
using System.Threading.Tasks;
using TestAttarClone.Application.Abstractions.Messaging;
using TestAttarClone.Application.Interfaces;
using TestAttarClone.SharedKernel.BaseTypes;

namespace TestAttarClone.Application.Commands.UserAddresses;

public sealed record DeleteUserAddressCommand(
    Guid AddressId,
    string UserId) : ICommand
{
    public sealed class Handler : ICommandHandler<DeleteUserAddressCommand>
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

        public async Task<Result> Handle(DeleteUserAddressCommand request, CancellationToken cancellationToken)
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

            var audit = _auditContext.Capture();
            address.Delete();
            address.UpdaterId = audit.UserId;
            address.UpdateDate = audit.Timestamp;
            address.Ip = audit.IpAddress;

            await _addressRepository.RemoveAsync(address, cancellationToken);

            return Result.Success();
        }
    }
}

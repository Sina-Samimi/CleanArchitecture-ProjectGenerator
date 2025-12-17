using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TestAttarClone.Application.Abstractions.Messaging;
using TestAttarClone.Application.Interfaces;
using TestAttarClone.Domain.Entities.Billing;
using TestAttarClone.Domain.Enums;
using TestAttarClone.SharedKernel.BaseTypes;

namespace TestAttarClone.Application.Commands.Orders;

public sealed record UpdateShipmentTrackingCommand(
    Guid Id,
    ShipmentStatus Status,
    DateTimeOffset StatusDate,
    string? TrackingNumber = null,
    string? Notes = null) : ICommand<Guid>
{
    public sealed class Handler : ICommandHandler<UpdateShipmentTrackingCommand, Guid>
    {
        private readonly IShipmentTrackingRepository _trackingRepository;
        private readonly IAuditContext _auditContext;

        public Handler(
            IShipmentTrackingRepository trackingRepository,
            IAuditContext auditContext)
        {
            _trackingRepository = trackingRepository;
            _auditContext = auditContext;
        }

        public async Task<Result<Guid>> Handle(UpdateShipmentTrackingCommand request, CancellationToken cancellationToken)
        {
            var tracking = await _trackingRepository.GetByIdAsync(request.Id, cancellationToken);
            if (tracking is null)
            {
                return Result<Guid>.Failure("پیگیری ارسال مورد نظر یافت نشد.");
            }

            var audit = _auditContext.Capture();

            tracking.UpdateStatus(
                request.Status,
                request.StatusDate,
                request.TrackingNumber,
                request.Notes);

            tracking.SetUpdatedBy(audit.UserId);
            tracking.UpdateDate = audit.Timestamp;

            await _trackingRepository.UpdateAsync(tracking, cancellationToken);

            return Result<Guid>.Success(tracking.Id);
        }
    }
}


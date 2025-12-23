using System;
using System.Threading;
using System.Threading.Tasks;
using MobiRooz.Application.Abstractions.Messaging;
using MobiRooz.Application.Interfaces;
using MobiRooz.SharedKernel.BaseTypes;

namespace MobiRooz.Application.Commands.Notifications;

public sealed record DeleteNotificationCommand(Guid NotificationId) : ICommand
{
    public sealed class Handler : ICommandHandler<DeleteNotificationCommand>
    {
        private readonly INotificationRepository _notificationRepository;

        public Handler(INotificationRepository notificationRepository)
        {
            _notificationRepository = notificationRepository;
        }

        public async Task<Result> Handle(DeleteNotificationCommand request, CancellationToken cancellationToken)
        {
            var notification = await _notificationRepository.GetByIdAsync(request.NotificationId, cancellationToken);
            if (notification == null)
            {
                return Result.Failure("اعلان یافت نشد.");
            }

            await _notificationRepository.DeleteAsync(request.NotificationId, cancellationToken);
            return Result.Success();
        }
    }
}

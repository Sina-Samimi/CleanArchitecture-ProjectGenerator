using System;
using System.Threading;
using System.Threading.Tasks;
using TestAttarClone.Application.Abstractions.Messaging;
using TestAttarClone.Application.Interfaces;
using TestAttarClone.SharedKernel.BaseTypes;

namespace TestAttarClone.Application.Commands.Notifications;

public sealed record UpdateNotificationCommand(
    Guid NotificationId,
    string Title,
    string Message,
    DateTimeOffset? ExpiresAt) : ICommand
{
    public sealed class Handler : ICommandHandler<UpdateNotificationCommand>
    {
        private readonly INotificationRepository _notificationRepository;

        public Handler(INotificationRepository notificationRepository)
        {
            _notificationRepository = notificationRepository;
        }

        public async Task<Result> Handle(UpdateNotificationCommand request, CancellationToken cancellationToken)
        {
            var notification = await _notificationRepository.GetByIdAsync(request.NotificationId, cancellationToken);
            if (notification == null)
            {
                return Result.Failure("اعلان یافت نشد.");
            }

            notification.UpdateContent(request.Title, request.Message);
            // Update expiry if provided
            notification.SetExpiresAt(request.ExpiresAt);
            await _notificationRepository.UpdateAsync(notification, cancellationToken);
            return Result.Success();
        }
    }
}

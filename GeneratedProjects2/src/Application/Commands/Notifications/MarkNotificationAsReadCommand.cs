using System;
using System.Threading;
using System.Threading.Tasks;
using LogsDtoCloneTest.Application.Abstractions.Messaging;
using LogsDtoCloneTest.Application.Interfaces;
using LogsDtoCloneTest.SharedKernel.BaseTypes;

namespace LogsDtoCloneTest.Application.Commands.Notifications;

public sealed record MarkNotificationAsReadCommand(Guid NotificationId, string UserId) : ICommand
{
    public sealed class Handler : ICommandHandler<MarkNotificationAsReadCommand>
    {
        private readonly INotificationRepository _notificationRepository;

        public Handler(INotificationRepository notificationRepository)
        {
            _notificationRepository = notificationRepository;
        }

        public async Task<Result> Handle(MarkNotificationAsReadCommand request, CancellationToken cancellationToken)
        {
            var userNotification = await _notificationRepository.GetUserNotificationAsync(
                request.NotificationId,
                request.UserId,
                cancellationToken);

            if (userNotification is null)
            {
                return Result.Failure("اعلان مورد نظر یافت نشد.");
            }

            userNotification.MarkAsRead();
            await _notificationRepository.UpdateUserNotificationAsync(userNotification, cancellationToken);

            return Result.Success();
        }
    }
}


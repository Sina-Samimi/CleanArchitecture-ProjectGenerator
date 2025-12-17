using System.Threading;
using System.Threading.Tasks;
using Attar.Application.Abstractions.Messaging;
using Attar.Application.Interfaces;
using Attar.SharedKernel.BaseTypes;

namespace Attar.Application.Queries.Notifications;

public sealed record GetUnreadNotificationsCountQuery(string UserId, string? UserRole = null) : IQuery<int>
{
    public sealed class Handler : IQueryHandler<GetUnreadNotificationsCountQuery, int>
    {
        private readonly INotificationRepository _notificationRepository;

        public Handler(INotificationRepository notificationRepository)
        {
            _notificationRepository = notificationRepository;
        }

        public async Task<Result<int>> Handle(GetUnreadNotificationsCountQuery request, CancellationToken cancellationToken)
        {
            var count = await _notificationRepository.GetUnreadCountAsync(request.UserId, request.UserRole, cancellationToken);
            return Result<int>.Success(count);
        }
    }
}


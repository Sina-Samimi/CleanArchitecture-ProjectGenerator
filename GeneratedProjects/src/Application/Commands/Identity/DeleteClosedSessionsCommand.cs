using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TestAttarClone.Application.Abstractions.Messaging;
using TestAttarClone.Application.Interfaces;
using TestAttarClone.SharedKernel.BaseTypes;

namespace TestAttarClone.Application.Commands.Identity;

public sealed record DeleteClosedSessionsCommand(string UserId) : ICommand<int>;

public sealed class DeleteClosedSessionsCommandHandler : ICommandHandler<DeleteClosedSessionsCommand, int>
{
    private readonly IUserSessionRepository _userSessionRepository;
    private readonly IAuditContext _auditContext;

    public DeleteClosedSessionsCommandHandler(
        IUserSessionRepository userSessionRepository,
        IAuditContext auditContext)
    {
        _userSessionRepository = userSessionRepository;
        _auditContext = auditContext;
    }

    public async Task<Result<int>> Handle(DeleteClosedSessionsCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.UserId))
        {
            return Result<int>.Failure("شناسه کاربر معتبر نیست.");
        }

        var sessions = await _userSessionRepository.GetByUserIdAsync(request.UserId, cancellationToken);
        
        // Filter closed sessions (sessions with SignedOutAt)
        var closedSessions = sessions
            .Where(s => s.SignedOutAt.HasValue && !s.IsDeleted)
            .ToList();

        if (closedSessions.Count == 0)
        {
            return Result<int>.Success(0);
        }

        // Soft delete closed sessions
        var audit = _auditContext.Capture();
        foreach (var session in closedSessions)
        {
            session.IsDeleted = true;
            session.RemoveDate = audit.Timestamp;
            session.UpdateDate = audit.Timestamp;
            session.UpdaterId = audit.UserId;
            session.Ip = audit.IpAddress;
        }

        // Update all sessions at once
        foreach (var session in closedSessions)
        {
            await _userSessionRepository.UpdateAsync(session, cancellationToken);
        }

        return Result<int>.Success(closedSessions.Count);
    }
}


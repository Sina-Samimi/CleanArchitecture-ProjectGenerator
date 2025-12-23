using System;
using System.Threading;
using System.Threading.Tasks;
using MobiRooz.Application.Abstractions.Messaging;
using MobiRooz.Application.Interfaces;
using MobiRooz.SharedKernel.BaseTypes;
using Microsoft.AspNetCore.Identity;

namespace MobiRooz.Application.Commands.Identity;

public sealed record CloseUserSessionCommand(string UserId, Guid SessionId) : ICommand;

public sealed class CloseUserSessionCommandHandler : ICommandHandler<CloseUserSessionCommand>
{
    private readonly IUserSessionRepository _userSessionRepository;
    private readonly UserManager<Domain.Entities.ApplicationUser> _userManager;

    public CloseUserSessionCommandHandler(
        IUserSessionRepository userSessionRepository,
        UserManager<Domain.Entities.ApplicationUser> userManager)
    {
        _userSessionRepository = userSessionRepository;
        _userManager = userManager;
    }

    public async Task<Result> Handle(CloseUserSessionCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.UserId))
        {
            return Result.Failure("شناسه کاربر معتبر نیست.");
        }

        if (request.SessionId == Guid.Empty)
        {
            return Result.Failure("شناسه session معتبر نیست.");
        }

        var session = await _userSessionRepository.GetByIdAsync(request.SessionId, cancellationToken);
        if (session is null || session.IsDeleted)
        {
            return Result.Failure("Session مورد نظر یافت نشد.");
        }

        if (session.UserId != request.UserId)
        {
            return Result.Failure("شما اجازه بستن این session را ندارید.");
        }

        if (session.SignedOutAt.HasValue)
        {
            return Result.Failure("این session قبلاً بسته شده است.");
        }

        // Close the session
        session.Close();
        await _userSessionRepository.UpdateAsync(session, cancellationToken);

        // Update Security Stamp to invalidate all sessions (this will force logout on other devices)
        // The middleware will check if the current session is closed and logout the user
        var user = await _userManager.FindByIdAsync(request.UserId);
        if (user is not null)
        {
            await _userManager.UpdateSecurityStampAsync(user);
        }

        return Result.Success();
    }
}


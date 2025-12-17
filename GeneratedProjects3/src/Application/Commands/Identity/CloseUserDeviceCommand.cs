using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LogTableRenameTest.Application.Abstractions.Messaging;
using LogTableRenameTest.Application.Interfaces;
using LogTableRenameTest.SharedKernel.BaseTypes;
using Microsoft.AspNetCore.Identity;

namespace LogTableRenameTest.Application.Commands.Identity;

public sealed record CloseUserDeviceCommand(string UserId, string DeviceKey) : ICommand<int>;

public sealed class CloseUserDeviceCommandHandler : ICommandHandler<CloseUserDeviceCommand, int>
{
    private readonly IUserSessionRepository _userSessionRepository;
    private readonly UserManager<Domain.Entities.ApplicationUser> _userManager;

    public CloseUserDeviceCommandHandler(
        IUserSessionRepository userSessionRepository,
        UserManager<Domain.Entities.ApplicationUser> userManager)
    {
        _userSessionRepository = userSessionRepository;
        _userManager = userManager;
    }

    public async Task<Result<int>> Handle(CloseUserDeviceCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.UserId))
        {
            return Result<int>.Failure("شناسه کاربر معتبر نیست.");
        }

        if (string.IsNullOrWhiteSpace(request.DeviceKey))
        {
            return Result<int>.Failure("کلید دستگاه معتبر نیست.");
        }

        var sessions = await _userSessionRepository.GetByUserIdAsync(request.UserId, cancellationToken);
        
        // Parse device key (DeviceType|ClientName)
        var parts = request.DeviceKey.Split('|');
        if (parts.Length != 2)
        {
            return Result<int>.Failure("فرمت کلید دستگاه معتبر نیست.");
        }

        var deviceType = parts[0];
        var clientName = parts[1];

        // Find all active sessions for this device
        var deviceSessions = sessions
            .Where(s => !s.IsDeleted && 
                       s.DeviceType == deviceType && 
                       s.ClientName == clientName &&
                       s.SignedOutAt == null)
            .ToList();

        if (deviceSessions.Count == 0)
        {
            return Result<int>.Success(0);
        }

        // Close all active sessions for this device
        foreach (var session in deviceSessions)
        {
            session.Close();
            await _userSessionRepository.UpdateAsync(session, cancellationToken);
        }

        // Update Security Stamp to invalidate all sessions (this will force logout on that device)
        var user = await _userManager.FindByIdAsync(request.UserId);
        if (user is not null)
        {
            await _userManager.UpdateSecurityStampAsync(user);
        }

        return Result<int>.Success(deviceSessions.Count);
    }
}


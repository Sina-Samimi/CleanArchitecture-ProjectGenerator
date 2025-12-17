using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Attar.Domain.Entities;

namespace Attar.Application.Interfaces;

public interface IUserSessionRepository
{
    Task AddAsync(UserSession session, CancellationToken cancellationToken);
    Task<Dictionary<string, DateTimeOffset>> GetLastSeenTimesAsync(IReadOnlyCollection<string> userIds, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<UserSession>> GetByUserIdAsync(string userId, CancellationToken cancellationToken);
    Task<UserSession?> GetByIdAsync(Guid sessionId, CancellationToken cancellationToken);
    Task UpdateAsync(UserSession session, CancellationToken cancellationToken);
}

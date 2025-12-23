using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MobiRooz.Application.Interfaces;
using MobiRooz.Domain.Entities;
using MobiRooz.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MobiRooz.Infrastructure.Persistence.Repositories;

public sealed class UserSessionRepository : IUserSessionRepository
{
    private readonly AppDbContext _dbContext;

    public UserSessionRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(UserSession session, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(session);

        await _dbContext.UserSessions.AddAsync(session, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<Dictionary<string, DateTimeOffset>> GetLastSeenTimesAsync(IReadOnlyCollection<string> userIds, CancellationToken cancellationToken)
    {
        if (userIds is null || userIds.Count == 0)
        {
            return new Dictionary<string, DateTimeOffset>();
        }

        var lastSeenTimes = await _dbContext.UserSessions
            .Where(session => userIds.Contains(session.UserId) && !session.IsDeleted && session.SignedOutAt == null)
            .GroupBy(session => session.UserId)
            .Select(g => new { UserId = g.Key, LastSeenAt = g.Max(s => s.LastSeenAt) })
            .ToDictionaryAsync(x => x.UserId, x => x.LastSeenAt, cancellationToken);

        return lastSeenTimes;
    }

    public async Task<IReadOnlyCollection<UserSession>> GetByUserIdAsync(string userId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Array.Empty<UserSession>();
        }

        return await _dbContext.UserSessions
            .AsNoTracking()
            .Where(session => session.UserId == userId && !session.IsDeleted)
            .OrderByDescending(session => session.SignedInAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<UserSession?> GetByIdAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        return await _dbContext.UserSessions
            .FirstOrDefaultAsync(session => session.Id == sessionId && !session.IsDeleted, cancellationToken);
    }

    public async Task UpdateAsync(UserSession session, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(session);

        _dbContext.UserSessions.Update(session);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}

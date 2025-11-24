using System;
using System.Threading;
using System.Threading.Tasks;
using Arsis.Application.Interfaces;
using Arsis.Domain.Entities;
using Arsis.Infrastructure.Persistence;

namespace Arsis.Infrastructure.Persistence.Repositories;

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
}

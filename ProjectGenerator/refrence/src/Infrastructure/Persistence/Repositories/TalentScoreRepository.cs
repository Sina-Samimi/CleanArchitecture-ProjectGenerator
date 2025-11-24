using System;
using System.Collections.Generic;
using Arsis.Application.Interfaces;
using Arsis.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Arsis.Infrastructure.Persistence.Repositories;

public sealed class TalentScoreRepository : ITalentScoreRepository
{
    private readonly AppDbContext _dbContext;
    private readonly IAuditContext _auditContext;

    public TalentScoreRepository(AppDbContext dbContext, IAuditContext auditContext)
    {
        _dbContext = dbContext;
        _auditContext = auditContext;
    }

    public async Task<IReadOnlyCollection<TalentScore>> GetTopScoresAsync(Guid userId, int count, CancellationToken cancellationToken)
    {
        return await _dbContext.TalentScores
            .Where(score => score.UserId == userId)
            .OrderByDescending(score => score.Score.Value)
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    public async Task SaveScoresAsync(IEnumerable<TalentScore> scores, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(scores);

        var audit = _auditContext.Capture();

        var useTransaction = _dbContext.Database.IsRelational();
        IDbContextTransaction? transaction = null;

        if (useTransaction)
        {
            transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        }

        try
        {
            foreach (var score in scores)
            {
                var existing = await _dbContext.TalentScores
                    .FirstOrDefaultAsync(s => s.UserId == score.UserId && s.TalentId == score.TalentId, cancellationToken);

                if (existing is null)
                {
                    score.CreatorId = audit.UserId;
                    score.CreateDate = audit.Timestamp;
                    score.UpdateDate = audit.Timestamp;
                    score.Ip = audit.IpAddress;
                    await _dbContext.TalentScores.AddAsync(score, cancellationToken);
                }
                else
                {
                    existing.UpdateScore(score.Score, score.CalculatedAt);
                    existing.UpdaterId = audit.UserId;
                    existing.UpdateDate = audit.Timestamp;
                    existing.Ip = audit.IpAddress;
                }
            }

            await _dbContext.SaveChangesAsync(cancellationToken);

            if (transaction is not null)
            {
                await transaction.CommitAsync(cancellationToken);
            }
        }
        finally
        {
            if (transaction is not null)
            {
                await transaction.DisposeAsync();
            }
        }
    }
}

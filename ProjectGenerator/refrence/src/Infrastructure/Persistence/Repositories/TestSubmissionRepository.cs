using System;
using System.Collections.Generic;
using Arsis.Application.Interfaces;
using Arsis.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Arsis.Infrastructure.Persistence.Repositories;

public sealed class TestSubmissionRepository : ITestSubmissionRepository
{
    private readonly AppDbContext _dbContext;
    private readonly IAuditContext _auditContext;

    public TestSubmissionRepository(AppDbContext dbContext, IAuditContext auditContext)
    {
        _dbContext = dbContext;
        _auditContext = auditContext;
    }

    public async Task SaveResponsesAsync(IEnumerable<UserResponse> responses, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(responses);

        var audit = _auditContext.Capture();

        var useTransaction = _dbContext.Database.IsRelational();
        IDbContextTransaction? transaction = null;

        if (useTransaction)
        {
            transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        }

        try
        {
            foreach (var response in responses)
            {
                var existing = await _dbContext.UserResponses
                    .FirstOrDefaultAsync(r => r.UserId == response.UserId && r.QuestionId == response.QuestionId, cancellationToken);

                if (existing is null)
                {
                    response.CreatorId = audit.UserId;
                    response.CreateDate = audit.Timestamp;
                    response.UpdateDate = audit.Timestamp;
                    response.Ip = audit.IpAddress;
                    await _dbContext.UserResponses.AddAsync(response, cancellationToken);
                }
                else
                {
                    existing.UpdateAnswer(response.Answer, response.SubmittedAt);
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

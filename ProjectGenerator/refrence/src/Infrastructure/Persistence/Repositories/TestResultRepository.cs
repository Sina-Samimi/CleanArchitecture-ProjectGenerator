using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arsis.Application.Interfaces;
using Arsis.Domain.Entities.Tests;
using Microsoft.EntityFrameworkCore;

namespace Arsis.Infrastructure.Persistence.Repositories;

public sealed class TestResultRepository : ITestResultRepository
{
    private readonly AppDbContext _dbContext;

    public TestResultRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<TestResult?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.TestResults
            .Include(r => r.Attempt)
            .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted, cancellationToken);
    }

    public async Task<List<TestResult>> GetByAttemptIdAsync(
        Guid attemptId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.TestResults
            .Where(r => r.AttemptId == attemptId && !r.IsDeleted)
            .OrderBy(r => r.Rank ?? int.MaxValue)
            .ThenByDescending(r => r.Score)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<TestResult>> GetUserResultsAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.TestResults
            .Include(r => r.Attempt)
                .ThenInclude(a => a.Test)
            .Where(r => r.Attempt.UserId == userId && !r.IsDeleted)
            .OrderByDescending(r => r.CreateDate)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(TestResult result, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(result);
        await _dbContext.TestResults.AddAsync(result, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task AddRangeAsync(List<TestResult> results, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(results);
        if (results.Count == 0) return;

        await _dbContext.TestResults.AddRangeAsync(results, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(TestResult result, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(result);
        _dbContext.TestResults.Update(result);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteByAttemptIdAsync(Guid attemptId, CancellationToken cancellationToken = default)
    {
        var results = await _dbContext.TestResults
            .Where(result => result.AttemptId == attemptId)
            .ToListAsync(cancellationToken);

        if (results.Count == 0)
        {
            return;
        }

        _dbContext.TestResults.RemoveRange(results);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.SaveChangesAsync(cancellationToken);
    }
}

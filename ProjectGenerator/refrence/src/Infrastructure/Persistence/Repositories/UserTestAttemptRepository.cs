using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arsis.Application.DTOs.Tests;
using Arsis.Application.Interfaces;
using Arsis.Domain.Entities.Tests;
using Arsis.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Arsis.Infrastructure.Persistence.Repositories;

public sealed class UserTestAttemptRepository : IUserTestAttemptRepository
{
    private readonly AppDbContext _dbContext;

    public UserTestAttemptRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<UserTestAttempt?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.UserTestAttempts
            .Include(a => a.Test)
            .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted, cancellationToken);
    }

    public async Task<UserTestAttempt?> GetByIdWithAnswersAsync(
        Guid id,
        CancellationToken cancellationToken = default,
        bool includeDetails = false,
        bool asTracking = false)
    {
        var query = _dbContext.UserTestAttempts.AsQueryable();

        if (includeDetails)
        {
            query = query
                .Include(a => a.Test)
                .Include(a => a.User)
                .Include(a => a.Answers)
                    .ThenInclude(ans => ans.Question)
                .Include(a => a.Answers)
                    .ThenInclude(ans => ans.SelectedOption);
        }
        else
        {
            query = query.Include(a => a.Answers);
        }

        query = asTracking ? query.AsTracking() : query.AsNoTracking();

        return await query.FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted, cancellationToken);
    }

    public async Task<List<UserTestAttempt>> GetUserAttemptsAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.UserTestAttempts
            .Include(a => a.Test)
            .Where(a => a.UserId == userId && !a.IsDeleted)
            .OrderByDescending(a => a.StartedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<UserTestAttempt>> GetUserAttemptsForTestAsync(
        string userId,
        Guid testId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.UserTestAttempts
            .Include(a => a.Test)
            .Where(a => a.UserId == userId && a.TestId == testId && !a.IsDeleted)
            .OrderByDescending(a => a.StartedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<UserTestAttempt?> GetLatestAttemptAsync(
        string userId,
        Guid testId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.UserTestAttempts
            .Include(a => a.Test)
            .Where(a => a.UserId == userId && a.TestId == testId && !a.IsDeleted)
            .OrderByDescending(a => a.StartedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<int> GetUserAttemptCountAsync(
        string userId,
        Guid testId,
        TestAttemptStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.UserTestAttempts
            .Where(a => a.UserId == userId && a.TestId == testId && !a.IsDeleted);

        if (status.HasValue)
        {
            query = query.Where(a => a.Status == status.Value);
        }

        return await query.CountAsync(cancellationToken);
    }

    public async Task<UserTestAttempt?> GetActiveAttemptAsync(
        string userId,
        Guid testId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.UserTestAttempts
            .Include(a => a.Test)
            .Include(a => a.Answers)
            .FirstOrDefaultAsync(
                a => a.UserId == userId && 
                     a.TestId == testId && 
                     a.Status == TestAttemptStatus.InProgress && 
                     !a.IsDeleted,
                cancellationToken);
    }

    public async Task<UserTestAttempt?> GetByInvoiceIdAsync(
        Guid invoiceId,
        string? userId = null,
        CancellationToken cancellationToken = default)
    {
        if (invoiceId == Guid.Empty)
        {
            return null;
        }

        var query = _dbContext.UserTestAttempts
            .Include(a => a.Test)
            .Where(a => a.InvoiceId == invoiceId && !a.IsDeleted);

        if (!string.IsNullOrWhiteSpace(userId))
        {
            query = query.Where(a => a.UserId == userId);
        }

        return await query
            .OrderByDescending(a => a.StartedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<(List<UserTestAttempt> Items, int TotalCount, TestAttemptStatisticsDto Statistics)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        Guid? testId = null,
        string? userId = null,
        TestAttemptStatus? status = null,
        DateTimeOffset? startedFrom = null,
        DateTimeOffset? startedTo = null,
        string? searchTerm = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.UserTestAttempts
            .Include(a => a.Test)
            .Include(a => a.User)
            .Where(a => !a.IsDeleted)
            .AsQueryable();

        if (testId.HasValue)
        {
            query = query.Where(a => a.TestId == testId.Value);
        }

        if (!string.IsNullOrEmpty(userId))
        {
            query = query.Where(a => a.UserId == userId);
        }

        if (status.HasValue)
        {
            query = query.Where(a => a.Status == status.Value);
        }

        if (startedFrom.HasValue)
        {
            query = query.Where(a => a.StartedAt >= startedFrom.Value);
        }

        if (startedTo.HasValue)
        {
            query = query.Where(a => a.StartedAt <= startedTo.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var likeTerm = $"%{searchTerm.Trim()}%";
            query = query.Where(a =>
                EF.Functions.Like(a.Test!.Title, likeTerm) ||
                (a.User!.FullName != null && EF.Functions.Like(a.User.FullName, likeTerm)) ||
                (a.User.Email != null && EF.Functions.Like(a.User.Email, likeTerm)) ||
                (a.User.PhoneNumber != null && EF.Functions.Like(a.User.PhoneNumber, likeTerm)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var statistics = TestAttemptStatisticsDto.Empty;

        if (totalCount > 0)
        {
            var aggregate = await query
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    TotalAttempts = g.Count(),
                    CompletedAttempts = g.Count(a => a.Status == TestAttemptStatus.Completed),
                    InProgressAttempts = g.Count(a => a.Status == TestAttemptStatus.InProgress),
                    CancelledAttempts = g.Count(a => a.Status == TestAttemptStatus.Cancelled),
                    ExpiredAttempts = g.Count(a => a.Status == TestAttemptStatus.Expired),
                    UniqueParticipants = g.Select(a => a.UserId).Distinct().Count(),
                    ScoreSum = g.Sum(a => a.ScorePercentage ?? 0m),
                    ScoreCount = g.Count(a => a.ScorePercentage.HasValue),
                    CompletionSum = g.Sum(a => a.CompletedAt.HasValue
                        ? EF.Functions.DateDiffMinute(a.StartedAt, a.CompletedAt!.Value)
                        : 0),
                    CompletionCount = g.Count(a => a.CompletedAt.HasValue),
                    FirstAttemptAt = g.Min(a => (DateTimeOffset?)a.StartedAt),
                    LastAttemptAt = g.Max(a => (DateTimeOffset?)a.StartedAt)
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (aggregate is not null)
            {
                var averageScore = aggregate.ScoreCount > 0
                    ? aggregate.ScoreSum / aggregate.ScoreCount
                    : (decimal?)null;

                var averageCompletion = aggregate.CompletionCount > 0
                    ? (double)aggregate.CompletionSum / aggregate.CompletionCount
                    : (double?)null;

                statistics = new TestAttemptStatisticsDto
                {
                    TotalAttempts = aggregate.TotalAttempts,
                    CompletedAttempts = aggregate.CompletedAttempts,
                    InProgressAttempts = aggregate.InProgressAttempts,
                    CancelledAttempts = aggregate.CancelledAttempts,
                    ExpiredAttempts = aggregate.ExpiredAttempts,
                    UniqueParticipants = aggregate.UniqueParticipants,
                    AverageScore = averageScore,
                    AverageCompletionMinutes = averageCompletion,
                    FirstAttemptAt = aggregate.FirstAttemptAt,
                    LastAttemptAt = aggregate.LastAttemptAt
                };
            }
        }

        var items = await query
            .OrderByDescending(a => a.StartedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount, statistics);
    }

    public async Task AddAsync(UserTestAttempt attempt, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(attempt);
        await _dbContext.UserTestAttempts.AddAsync(attempt, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(UserTestAttempt attempt, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(attempt);

        try
        {
            _dbContext.ChangeTracker.Clear();

            var attemptEntry = _dbContext.UserTestAttempts.Attach(attempt);
            attemptEntry.State = EntityState.Modified;

            if (attempt.Test is not null)
            {
                _dbContext.Entry(attempt.Test).State = EntityState.Unchanged;
            }

            if (attempt.User is not null)
            {
                _dbContext.Entry(attempt.User).State = EntityState.Unchanged;
            }

            var answers = attempt.Answers;

            if (answers.Count > 0)
            {
                var persistedAnswerIds = answers
                    .Where(answer => answer.Id != Guid.Empty)
                    .Select(answer => answer.Id)
                    .ToArray();

                HashSet<Guid> existingAnswerSet = new();

                if (persistedAnswerIds.Length > 0)
                {
                    var existingAnswerIds = await _dbContext.UserTestAnswers
                        .Where(answer => persistedAnswerIds.Contains(answer.Id))
                        .Select(answer => answer.Id)
                        .ToListAsync(cancellationToken);

                    existingAnswerSet = existingAnswerIds.ToHashSet();
                }

                foreach (var answer in answers)
                {
                    if (answer.Question is not null)
                    {
                        var questionEntry = _dbContext.Entry(answer.Question);
                        questionEntry.State = EntityState.Unchanged;

                        if (answer.Question.Test is not null)
                        {
                            _dbContext.Entry(answer.Question.Test).State = EntityState.Unchanged;
                        }
                    }

                    if (answer.SelectedOption is not null)
                    {
                        _dbContext.Entry(answer.SelectedOption).State = EntityState.Unchanged;
                    }

                    var answerEntry = _dbContext.Entry(answer);

                    if (answer.Id != Guid.Empty && existingAnswerSet.Contains(answer.Id))
                    {
                        answerEntry.State = EntityState.Modified;
                        answerEntry.Property(a => a.AttemptId).IsModified = false;
                        answerEntry.Property(a => a.QuestionId).IsModified = false;
                    }
                    else
                    {
                        answerEntry.State = EntityState.Added;
                    }
                }
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            _dbContext.ChangeTracker.Clear();
            throw;
        }
        catch (DbUpdateException)
        {
            _dbContext.ChangeTracker.Clear();
            throw;
        }
        finally
        {
            _dbContext.ChangeTracker.Clear();
        }
    }

    public async Task DeleteAsync(UserTestAttempt attempt, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(attempt);

        var existingAttempt = await _dbContext.UserTestAttempts
            .Include(a => a.Answers)
            .FirstOrDefaultAsync(a => a.Id == attempt.Id, cancellationToken);

        if (existingAttempt is null)
        {
            return;
        }

        var relatedResults = await _dbContext.TestResults
            .Where(result => result.AttemptId == attempt.Id)
            .ToListAsync(cancellationToken);

        if (relatedResults.Count > 0)
        {
            _dbContext.TestResults.RemoveRange(relatedResults);
        }

        _dbContext.UserTestAttempts.Remove(existingAttempt);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.SaveChangesAsync(cancellationToken);
    }
}

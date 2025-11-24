using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arsis.Application.Interfaces;
using Arsis.Domain.Entities.Tests;
using Arsis.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Arsis.Infrastructure.Persistence.Repositories;

public sealed class TestRepository : ITestRepository
{
    private readonly AppDbContext _dbContext;

    public TestRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Test?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Tests
            .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted, cancellationToken);
    }

    public async Task<Test?> GetByIdWithQuestionsAsync(
        Guid id,
        CancellationToken cancellationToken = default,
        bool asTracking = true)
    {
        var query = _dbContext.Tests
            .Include(t => t.Category)
            .Include(t => t.Questions)
                .ThenInclude(q => q.Options)
            .Include(t => t.Attempts)
            .AsQueryable();

        query = asTracking ? query.AsTracking() : query.AsNoTracking();

        return await query.FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted, cancellationToken);
    }

    public async Task<List<Test>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Tests
            .Where(t => !t.IsDeleted)
            .Include(t => t.Category)
            .Include(t => t.Questions)
            .Include(t => t.Attempts)
            .OrderByDescending(t => t.CreateDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Test>> GetByTypeAsync(TestType type, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Tests
            .Where(t => t.Type == type && !t.IsDeleted)
            .Include(t => t.Questions)
            .Include(t => t.Attempts)
            .OrderByDescending(t => t.CreateDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Test>> GetPublishedAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Tests
            .Where(t => t.Status == TestStatus.Published && !t.IsDeleted)
            .Include(t => t.Questions)
            .Include(t => t.Attempts)
            .OrderByDescending(t => t.CreateDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<(List<Test> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        TestType? type = null,
        TestStatus? status = null,
        string? searchTerm = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Tests
            .Where(t => !t.IsDeleted)
            .Include(t => t.Category)
            .Include(t => t.Questions)
            .Include(t => t.Attempts)
            .AsQueryable();

        if (type.HasValue)
        {
            query = query.Where(t => t.Type == type.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(t => t.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var search = searchTerm.Trim().ToLower();
            query = query.Where(t => 
                t.Title.ToLower().Contains(search) || 
                t.Description.ToLower().Contains(search));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(t => t.CreateDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task AddAsync(Test test, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(test);
        await _dbContext.Tests.AddAsync(test, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public void AddQuestionGraph(TestQuestion question)
    {
        ArgumentNullException.ThrowIfNull(question);

        var questionEntry = _dbContext.Entry(question);
        questionEntry.State = EntityState.Added;

        foreach (var option in question.Options)
        {
            var optionEntry = _dbContext.Entry(option);
            optionEntry.State = EntityState.Added;
        }
    }

    public async Task UpdateAsync(Test test, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(test);

        try
        {
            var entry = _dbContext.Entry(test);

            if (entry.State == EntityState.Detached)
            {
                _dbContext.Tests.Attach(test);
                entry = _dbContext.Entry(test);
            }

            entry.State = EntityState.Modified;

            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        finally
        {
            _dbContext.ChangeTracker.Clear();
        }
    }

    public async Task DeleteAsync(Test test, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(test);
        test.IsDeleted = true;
        test.RemoveDate = DateTimeOffset.UtcNow;
        _dbContext.Tests.Update(test);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.SaveChangesAsync(cancellationToken);
    }
}

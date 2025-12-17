using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LogsDtoCloneTest.Application.Interfaces;
using LogsDtoCloneTest.Domain.Entities.Blogs;
using Microsoft.EntityFrameworkCore;

namespace LogsDtoCloneTest.Infrastructure.Persistence.Repositories;

public sealed class BlogCategoryRepository : IBlogCategoryRepository
{
    private readonly AppDbContext _dbContext;

    public BlogCategoryRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<BlogCategory>> GetAllAsync(CancellationToken cancellationToken)
        => await _dbContext.BlogCategories
            .AsNoTracking()
            .Where(category => !category.IsDeleted)
            .ToListAsync(cancellationToken);

    public async Task<BlogCategory?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => await _dbContext.BlogCategories
            .AsNoTracking()
            .FirstOrDefaultAsync(category => category.Id == id && !category.IsDeleted, cancellationToken);

    public async Task<BlogCategory?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken)
        => await _dbContext.BlogCategories
            .FirstOrDefaultAsync(category => category.Id == id && !category.IsDeleted, cancellationToken);

    public async Task AddAsync(BlogCategory category, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(category);

        if (category.Parent is not null)
        {
            _dbContext.Attach(category.Parent);
        }

        await _dbContext.BlogCategories.AddAsync(category, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(BlogCategory category, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(category);

        if (category.Parent is not null)
        {
            _dbContext.Attach(category.Parent);
        }

        _dbContext.BlogCategories.Update(category);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveAsync(BlogCategory category, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(category);

        _dbContext.BlogCategories.Update(category);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<Guid>> GetDescendantIdsAsync(Guid id, CancellationToken cancellationToken)
    {
        var categories = await _dbContext.BlogCategories
            .AsNoTracking()
            .Where(category => !category.IsDeleted)
            .Select(category => new { category.Id, category.ParentId })
            .ToListAsync(cancellationToken);

        if (categories.Count == 0)
        {
            return Array.Empty<Guid>();
        }

        var lookup = categories
            .GroupBy(category => category.ParentId)
            .ToDictionary(
                group => group.Key,
                group => group.Select(category => category.Id).ToArray());

        var stack = new Stack<Guid>();
        var result = new HashSet<Guid>();

        stack.Push(id);

        while (stack.Count > 0)
        {
            var current = stack.Pop();
            if (!result.Add(current))
            {
                continue;
            }

            var parentKey = (Guid?)current;
            if (!lookup.TryGetValue(parentKey, out var children))
            {
                continue;
            }

            foreach (var child in children)
            {
                stack.Push(child);
            }
        }

        return result.ToArray();
    }

    public async Task<bool> ExistsBySlugAsync(string slug, Guid? excludeId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return false;
        }

        var normalizedSlug = slug.Trim();

        var query = _dbContext.BlogCategories
            .AsNoTracking()
            .Where(category => !category.IsDeleted && category.Slug == normalizedSlug);

        if (excludeId.HasValue)
        {
            var excluded = excludeId.Value;
            query = query.Where(category => category.Id != excluded);
        }

        return await query.AnyAsync(cancellationToken);
    }
}

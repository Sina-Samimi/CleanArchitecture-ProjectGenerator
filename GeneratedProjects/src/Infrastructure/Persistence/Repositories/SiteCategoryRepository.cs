using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TestAttarClone.Application.Interfaces;
using TestAttarClone.Domain.Entities.Catalog;
using TestAttarClone.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace TestAttarClone.Infrastructure.Persistence.Repositories;

public sealed class SiteCategoryRepository : ISiteCategoryRepository
{
    private readonly AppDbContext _dbContext;

    public SiteCategoryRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(SiteCategory category, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(category);

        if (category.Parent is not null)
        {
            _dbContext.Attach(category.Parent);
        }

        await _dbContext.SiteCategories.AddAsync(category, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> ExistsBySlugAsync(string slug, CategoryScope scope, Guid? excludeId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return false;
        }

        var normalizedSlug = slug.Trim();

        var query = _dbContext.SiteCategories
            .AsNoTracking()
            .Where(category => !category.IsDeleted && category.Scope == scope && category.Slug == normalizedSlug);

        if (excludeId.HasValue)
        {
            var excluded = excludeId.Value;
            query = query.Where(category => category.Id != excluded);
        }

        return await query.AnyAsync(cancellationToken);
    }

    public async Task<SiteCategory?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => await _dbContext.SiteCategories
            .AsNoTracking()
            .FirstOrDefaultAsync(category => category.Id == id && !category.IsDeleted, cancellationToken);

    public async Task<IReadOnlyCollection<Guid>> GetDescendantIdsAsync(Guid id, CancellationToken cancellationToken)
    {
        var root = await _dbContext.SiteCategories
            .AsNoTracking()
            .FirstOrDefaultAsync(category => category.Id == id && !category.IsDeleted, cancellationToken);

        if (root is null)
        {
            return Array.Empty<Guid>();
        }

        var categories = await _dbContext.SiteCategories
            .AsNoTracking()
            .Where(category => !category.IsDeleted && category.Scope == root.Scope)
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

    public async Task<IReadOnlyCollection<SiteCategory>> GetByScopeAsync(CategoryScope scope, CancellationToken cancellationToken)
    {
        return await _dbContext.SiteCategories
            .AsNoTracking()
            .Where(category => !category.IsDeleted && category.Scope == scope)
            .OrderBy(category => category.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<SiteCategory>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.SiteCategories
            .AsNoTracking()
            .Where(category => !category.IsDeleted)
            .OrderBy(category => category.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<SiteCategory>> GetTreeAsync(CategoryScope scope, CancellationToken cancellationToken)
    {
        var categories = await _dbContext.SiteCategories
            .AsNoTracking()
            .Where(category => !category.IsDeleted && category.Scope == scope)
            .OrderBy(category => category.Name)
            .ToListAsync(cancellationToken);

        if (categories.Count == 0)
        {
            return categories;
        }

        var lookup = categories.ToDictionary(category => category.Id);

        foreach (var category in categories)
        {
            if (category.ParentId is Guid parentId && lookup.TryGetValue(parentId, out var parent))
            {
                parent.AddChild(category);
            }
        }

        return categories;
    }

    public async Task<bool> HasChildrenAsync(Guid id, CancellationToken cancellationToken)
        => await _dbContext.SiteCategories
            .AsNoTracking()
            .AnyAsync(category => category.ParentId == id && !category.IsDeleted, cancellationToken);

    public async Task RemoveAsync(SiteCategory category, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(category);

        _dbContext.SiteCategories.Update(category);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(SiteCategory category, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(category);

        if (category.Parent is not null)
        {
            _dbContext.Attach(category.Parent);
        }

        _dbContext.SiteCategories.Update(category);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}

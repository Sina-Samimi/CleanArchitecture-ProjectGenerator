using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arsis.Application.DTOs.Catalog;
using Arsis.Application.Interfaces;
using Arsis.Domain.Entities.Catalog;
using Microsoft.EntityFrameworkCore;

namespace Arsis.Infrastructure.Persistence.Repositories;

public sealed class ProductRepository : IProductRepository
{
    private readonly AppDbContext _dbContext;

    public ProductRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(Product product, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(product);

        AttachCategory(product);

        await _dbContext.Products.AddAsync(product, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> ExistsBySlugAsync(string slug, Guid? excludeId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return false;
        }

        var normalizedSlug = slug.Trim();

        var query = _dbContext.Products
            .AsNoTracking()
            .Where(product => !product.IsDeleted && product.SeoSlug == normalizedSlug);

        if (excludeId.HasValue)
        {
            var excluded = excludeId.Value;
            query = query.Where(product => product.Id != excluded);
        }

        return await query.AnyAsync(cancellationToken);
    }

    public async Task<bool> ExistsInCategoriesAsync(IReadOnlyCollection<Guid> categoryIds, CancellationToken cancellationToken)
    {
        if (categoryIds is null || categoryIds.Count == 0)
        {
            return false;
        }

        return await _dbContext.Products
            .AsNoTracking()
            .AnyAsync(product => !product.IsDeleted && categoryIds.Contains(product.CategoryId), cancellationToken);
    }

    public async Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => await _dbContext.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(product => product.Id == id && !product.IsDeleted, cancellationToken);

    public async Task<Product?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken)
        => await _dbContext.Products
            .Include(product => product.Category)
            .Include(product => product.Gallery)
            .Include(product => product.ExecutionSteps)
            .Include(product => product.Faqs)
            .Include(product => product.Comments)
            .FirstOrDefaultAsync(product => product.Id == id && !product.IsDeleted, cancellationToken);

    public async Task<ProductListResultDto> GetListAsync(
        ProductListFilterDto filter,
        IReadOnlyCollection<Guid>? categoryIds,
        CancellationToken cancellationToken)
    {
        var baseQuery = _dbContext.Products
            .AsNoTracking()
            .Where(product => !product.IsDeleted);

        var totalCount = await baseQuery.CountAsync(cancellationToken);

        var filteredQuery = ApplyFilters(baseQuery, filter, categoryIds);

        var filteredCount = await filteredQuery.CountAsync(cancellationToken);

        var page = filter.Page <= 0 ? 1 : filter.Page;
        var pageSize = filter.PageSize <= 0 ? 12 : Math.Clamp(filter.PageSize, 5, 100);

        var skip = (page - 1) * pageSize;
        if (skip < 0)
        {
            skip = 0;
        }

        var items = await filteredQuery
            .OrderByDescending(product => product.UpdateDate)
            .ThenBy(product => product.Name)
            .Skip(skip)
            .Take(pageSize)
            .Select(product => new ProductListItemDto(
                product.Id,
                product.Name,
                product.Type,
                product.Price,
                product.CompareAtPrice,
                product.IsPublished,
                product.PublishedAt,
                product.CategoryId,
                product.Category.Name,
                product.FeaturedImagePath,
                product.TagList ?? string.Empty,
                product.UpdateDate))
            .ToListAsync(cancellationToken);

        var totalPages = filteredCount == 0 ? 1 : (int)Math.Ceiling(filteredCount / (double)pageSize);
        var pageNumber = page > totalPages ? totalPages : page;
        if (pageNumber <= 0)
        {
            pageNumber = 1;
        }

        return new ProductListResultDto(
            items,
            totalCount,
            filteredCount,
            pageNumber,
            pageSize,
            totalPages);
    }

    public async Task<IReadOnlyCollection<Product>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken)
    {
        if (ids is null || ids.Count == 0)
        {
            return Array.Empty<Product>();
        }

        var normalizedIds = ids
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToArray();

        if (normalizedIds.Length == 0)
        {
            return Array.Empty<Product>();
        }

        var products = await _dbContext.Products
            .AsNoTracking()
            .Include(product => product.Category)
            .Where(product => !product.IsDeleted && normalizedIds.Contains(product.Id))
            .ToListAsync(cancellationToken);

        return products;
    }

    public async Task<IReadOnlyCollection<string>> GetAllTagsAsync(CancellationToken cancellationToken)
    {
        var tagLists = await _dbContext.Products
            .AsNoTracking()
            .Where(product => !product.IsDeleted && product.TagList != null && product.TagList != string.Empty)
            .Select(product => product.TagList!)
            .ToListAsync(cancellationToken);

        if (tagLists.Count == 0)
        {
            return Array.Empty<string>();
        }

        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var tagList in tagLists)
        {
            var tokens = tagList.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var token in tokens)
            {
                result.Add(token);
            }
        }

        return result.ToArray();
    }

    public async Task<IReadOnlyCollection<ProductListItemDto>> GetByTeacherAsync(string teacherId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(teacherId))
        {
            return Array.Empty<ProductListItemDto>();
        }

        var normalizedTeacherId = teacherId.Trim();

        var items = await _dbContext.Products
            .AsNoTracking()
            .Where(product => !product.IsDeleted
                && (product.CreatorId == normalizedTeacherId || product.TeacherId == normalizedTeacherId))
            .OrderByDescending(product => product.UpdateDate)
            .ThenBy(product => product.Name)
            .Select(product => new ProductListItemDto(
                product.Id,
                product.Name,
                product.Type,
                product.Price,
                product.CompareAtPrice,
                product.IsPublished,
                product.PublishedAt,
                product.CategoryId,
                product.Category.Name,
                product.FeaturedImagePath,
                product.TagList ?? string.Empty,
                product.UpdateDate))
            .ToListAsync(cancellationToken);

        return items;
    }

    public async Task<IReadOnlyCollection<ProductExecutionStepSummaryDto>> GetExecutionStepSummariesAsync(
        CancellationToken cancellationToken)
    {
        var summaries = await _dbContext.Products
            .AsNoTracking()
            .Where(product => !product.IsDeleted)
            .OrderBy(product => product.Name)
            .Select(product => new ProductExecutionStepSummaryDto(
                product.Id,
                product.Name,
                product.Category != null ? product.Category.Name : string.Empty,
                product.Type,
                product.IsPublished,
                product.ExecutionSteps.Count(step => !step.IsDeleted),
                product.UpdateDate))
            .ToListAsync(cancellationToken);

        return summaries;
    }

    public async Task RemoveAsync(Product product, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(product);

        _dbContext.Products.Update(product);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Product product, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(product);

        AttachCategory(product);

        var entry = _dbContext.Entry(product);
        if (entry.State == EntityState.Detached)
        {
            _dbContext.Products.Attach(product);
            entry.State = EntityState.Modified;
        }

        await SyncExecutionStepsAsync(product, cancellationToken);
        await SyncFaqsAsync(product, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SyncExecutionStepsAsync(Product product, CancellationToken cancellationToken)
    {
        if (product.ExecutionSteps.Count == 0)
        {
            return;
        }

        var existingIds = await _dbContext.ProductExecutionSteps
            .AsNoTracking()
            .Where(step => step.ProductId == product.Id)
            .Select(step => step.Id)
            .ToListAsync(cancellationToken);
        var existingLookup = existingIds.Count == 0 ? null : existingIds.ToHashSet();

        foreach (var step in product.ExecutionSteps)
        {
            var stepEntry = _dbContext.Entry(step);

            if (stepEntry.State == EntityState.Detached)
            {
                if (existingLookup is not null && existingLookup.Contains(step.Id))
                {
                    _dbContext.ProductExecutionSteps.Attach(step);
                    stepEntry.State = EntityState.Modified;
                }
                else
                {
                    _dbContext.ProductExecutionSteps.Add(step);
                }

                continue;
            }

            var existsInDatabase = existingLookup is not null && existingLookup.Contains(step.Id);

            if (!existsInDatabase)
            {
                stepEntry.State = EntityState.Added;
            }
        }
    }

    private async Task SyncFaqsAsync(Product product, CancellationToken cancellationToken)
    {
        if (product.Faqs.Count == 0)
        {
            return;
        }

        var existingIds = await _dbContext.ProductFaqs
            .AsNoTracking()
            .Where(faq => faq.ProductId == product.Id)
            .Select(faq => faq.Id)
            .ToListAsync(cancellationToken);

        var existingLookup = existingIds.Count == 0 ? null : existingIds.ToHashSet();

        foreach (var faq in product.Faqs)
        {
            var faqEntry = _dbContext.Entry(faq);

            if (faqEntry.State == EntityState.Detached)
            {
                if (existingLookup is not null && existingLookup.Contains(faq.Id))
                {
                    _dbContext.ProductFaqs.Attach(faq);
                    faqEntry.State = EntityState.Modified;
                }
                else
                {
                    _dbContext.ProductFaqs.Add(faq);
                }

                continue;
            }

            var existsInDatabase = existingLookup is not null && existingLookup.Contains(faq.Id);

            if (!existsInDatabase)
            {
                faqEntry.State = EntityState.Added;
            }
        }
    }

    private void AttachCategory(Product product)
    {
        if (product.Category is null)
        {
            return;
        }

        var trackedCategory = _dbContext.ChangeTracker
            .Entries<SiteCategory>()
            .FirstOrDefault(entry => entry.Entity.Id == product.Category.Id);

        if (trackedCategory is null)
        {
            _dbContext.Attach(product.Category);
        }
        else if (!ReferenceEquals(product.Category, trackedCategory.Entity))
        {
            var originalUpdate = product.UpdateDate;
            product.SetCategory(trackedCategory.Entity);
            product.UpdateDate = originalUpdate;
        }
    }

    private static IQueryable<Product> ApplyFilters(
        IQueryable<Product> query,
        ProductListFilterDto filter,
        IReadOnlyCollection<Guid>? categoryIds)
    {
        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var search = filter.SearchTerm.Trim();
            query = query.Where(product =>
                product.Name.Contains(search) ||
                product.Summary.Contains(search) ||
                product.Description.Contains(search));
        }

        if (categoryIds is not null && categoryIds.Count > 0)
        {
            query = query.Where(product => categoryIds.Contains(product.CategoryId));
        }

        if (filter.Type.HasValue)
        {
            var type = filter.Type.Value;
            query = query.Where(product => product.Type == type);
        }

        if (filter.IsPublished.HasValue)
        {
            if (filter.IsPublished.Value)
            {
                query = query.Where(product => product.IsPublished);
            }
            else
            {
                query = query.Where(product => !product.IsPublished);
            }
        }

        if (filter.MinPrice.HasValue)
        {
            var minPrice = filter.MinPrice.Value;
            query = query.Where(product => product.Price >= minPrice);
        }

        if (filter.MaxPrice.HasValue)
        {
            var maxPrice = filter.MaxPrice.Value;
            query = query.Where(product => product.Price <= maxPrice);
        }

        return query;
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arsis.Application.DTOs.Blogs;
using Arsis.Application.Interfaces;
using Arsis.Domain.Entities.Blogs;
using Arsis.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Arsis.Infrastructure.Persistence.Repositories;

public sealed class BlogRepository : IBlogRepository
{
    private readonly AppDbContext _dbContext;

    public BlogRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(Blog blog, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(blog);

        if (blog.Category is not null)
        {
            var trackedCategory = _dbContext.ChangeTracker
                .Entries<BlogCategory>()
                .FirstOrDefault(entry => entry.Entity.Id == blog.Category.Id);

            if (trackedCategory is null)
            {
                _dbContext.Attach(blog.Category);
            }
            else if (!ReferenceEquals(blog.Category, trackedCategory.Entity))
            {
                var originalUpdateDate = blog.UpdateDate;
                blog.SetCategory(trackedCategory.Entity);
                blog.UpdateDate = originalUpdateDate;
            }
        }

        if (blog.Author is not null)
        {
            var trackedAuthor = _dbContext.ChangeTracker
                .Entries<BlogAuthor>()
                .FirstOrDefault(entry => entry.Entity.Id == blog.Author.Id);

            if (trackedAuthor is null)
            {
                _dbContext.Attach(blog.Author);
            }
            else if (!ReferenceEquals(blog.Author, trackedAuthor.Entity))
            {
                var originalUpdateDate = blog.UpdateDate;
                blog.SetAuthor(trackedAuthor.Entity);
                blog.UpdateDate = originalUpdateDate;
            }
        }

        await _dbContext.Blogs.AddAsync(blog, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<Blog?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => await _dbContext.Blogs
            .AsNoTracking()
            .FirstOrDefaultAsync(blog => blog.Id == id && !blog.IsDeleted, cancellationToken);

    public async Task<Blog?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken)
        => await _dbContext.Blogs
            .Include(blog => blog.Category)
            .Include(blog => blog.Author)
            .Include(blog => blog.Comments)
            .Include(blog => blog.Views)
            .FirstOrDefaultAsync(blog => blog.Id == id && !blog.IsDeleted, cancellationToken);

    public async Task RemoveAsync(Blog blog, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(blog);

        _dbContext.Blogs.Update(blog);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Blog blog, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(blog);

        if (blog.Category is not null)
        {
            var trackedCategory = _dbContext.ChangeTracker
                .Entries<BlogCategory>()
                .FirstOrDefault(entry => entry.Entity.Id == blog.Category.Id);

            if (trackedCategory is null)
            {
                _dbContext.Attach(blog.Category);
            }
            else if (!ReferenceEquals(blog.Category, trackedCategory.Entity))
            {
                var originalUpdateDate = blog.UpdateDate;
                blog.SetCategory(trackedCategory.Entity);
                blog.UpdateDate = originalUpdateDate;
            }
        }

        if (blog.Author is not null)
        {
            var trackedAuthor = _dbContext.ChangeTracker
                .Entries<BlogAuthor>()
                .FirstOrDefault(entry => entry.Entity.Id == blog.Author.Id);

            if (trackedAuthor is null)
            {
                _dbContext.Attach(blog.Author);
            }
            else if (!ReferenceEquals(blog.Author, trackedAuthor.Entity))
            {
                var originalUpdateDate = blog.UpdateDate;
                blog.SetAuthor(trackedAuthor.Entity);
                blog.UpdateDate = originalUpdateDate;
            }
        }

        _dbContext.Blogs.Update(blog);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<BlogListResultDto> GetListAsync(
        BlogListFilterDto filter,
        IReadOnlyCollection<Guid>? categoryIds,
        CancellationToken cancellationToken)
    {
        var baseQuery = _dbContext.Blogs
            .AsNoTracking()
            .Where(blog => !blog.IsDeleted);

        var totalCount = await baseQuery.CountAsync(cancellationToken);

        var filteredQuery = ApplyFilters(baseQuery, filter, categoryIds);

        var filteredCount = await filteredQuery.CountAsync(cancellationToken);

        var statisticsItems = await filteredQuery
            .Select(blog => new StatisticsProjection(
                blog.Status,
                blog.LikeCount,
                blog.DislikeCount,
                blog.ReadingTimeMinutes,
                blog.Views.Count))
            .ToListAsync(cancellationToken);

        var statistics = BuildStatistics(statisticsItems);

        var skip = (filter.Page - 1) * filter.PageSize;
        if (skip < 0)
        {
            skip = 0;
        }

        var items = await filteredQuery
            .OrderByDescending(blog => blog.UpdateDate)
            .ThenByDescending(blog => blog.CreateDate)
            .Skip(skip)
            .Take(filter.PageSize)
            .Select(blog => new BlogListItemDto(
                blog.Id,
                blog.Title,
                blog.Category.Name,
                blog.CategoryId,
                blog.Author.DisplayName,
                blog.AuthorId,
                blog.Status,
                blog.PublishedAt,
                blog.ReadingTimeMinutes,
                blog.LikeCount,
                blog.DislikeCount,
                blog.Comments.Count(comment => comment.IsApproved),
                blog.Views.Count,
                blog.UpdateDate,
                blog.FeaturedImagePath,
                blog.Robots,
                blog.TagList ?? string.Empty))
            .ToListAsync(cancellationToken);

        var totalPages = filteredCount == 0 ? 1 : (int)Math.Ceiling(filteredCount / (double)filter.PageSize);
        var pageNumber = filter.Page > totalPages ? totalPages : filter.Page;
        if (pageNumber <= 0)
        {
            pageNumber = 1;
        }

        return new BlogListResultDto(
            items,
            totalCount,
            filteredCount,
            pageNumber,
            filter.PageSize,
            totalPages,
            statistics);
    }

    public async Task<bool> ExistsInCategoriesAsync(IReadOnlyCollection<Guid> categoryIds, CancellationToken cancellationToken)
    {
        if (categoryIds is null || categoryIds.Count == 0)
        {
            return false;
        }

        var scope = categoryIds.ToArray();

        return await _dbContext.Blogs
            .AsNoTracking()
            .AnyAsync(blog => !blog.IsDeleted && scope.Contains(blog.CategoryId), cancellationToken);
    }

    public async Task<bool> ExistsByAuthorAsync(Guid authorId, CancellationToken cancellationToken)
    {
        if (authorId == Guid.Empty)
        {
            return false;
        }

        return await _dbContext.Blogs
            .AsNoTracking()
            .AnyAsync(blog => !blog.IsDeleted && blog.AuthorId == authorId, cancellationToken);
    }

    private static IQueryable<Blog> ApplyFilters(
        IQueryable<Blog> query,
        BlogListFilterDto filter,
        IReadOnlyCollection<Guid>? categoryIds)
    {
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim();
            query = query.Where(blog =>
                blog.Title.Contains(search) ||
                blog.Summary.Contains(search) ||
                blog.SeoTitle.Contains(search) ||
                blog.SeoSlug.Contains(search));
        }

        if (filter.Status.HasValue)
        {
            query = query.Where(blog => blog.Status == filter.Status.Value);
        }

        if (filter.AuthorId.HasValue)
        {
            query = query.Where(blog => blog.AuthorId == filter.AuthorId.Value);
        }

        if (categoryIds is not null && categoryIds.Count > 0)
        {
            var scope = categoryIds.ToArray();
            query = query.Where(blog => scope.Contains(blog.CategoryId));
        }

        if (filter.FromDate.HasValue)
        {
            var from = filter.FromDate.Value.ToDateTime(TimeOnly.MinValue);
            var fromOffset = new DateTimeOffset(from, TimeSpan.Zero);
            query = query.Where(blog => blog.CreateDate >= fromOffset);
        }

        if (filter.ToDate.HasValue)
        {
            var to = filter.ToDate.Value.ToDateTime(TimeOnly.MaxValue);
            var toOffset = new DateTimeOffset(to, TimeSpan.Zero);
            query = query.Where(blog => blog.CreateDate <= toOffset);
        }

        return query;
    }

    private static BlogStatisticsDto BuildStatistics(IReadOnlyCollection<StatisticsProjection> items)
    {
        if (items.Count == 0)
        {
            return new BlogStatisticsDto(0, 0, 0, 0, 0, 0, 0, 0);
        }

        var total = items.Count;
        var published = 0;
        var draft = 0;
        var trash = 0;
        var totalLikes = 0;
        var totalDislikes = 0;
        var totalViews = 0;
        var readingTimeSum = 0;

        foreach (var item in items)
        {
            switch (item.Status)
            {
                case BlogStatus.Published:
                    published++;
                    break;
                case BlogStatus.Draft:
                    draft++;
                    break;
                case BlogStatus.Trash:
                    trash++;
                    break;
            }

            totalLikes += item.LikeCount;
            totalDislikes += item.DislikeCount;
            totalViews += item.ViewCount;
            readingTimeSum += item.ReadingTimeMinutes;
        }

        var averageReading = total == 0 ? 0 : readingTimeSum / (double)total;

        return new BlogStatisticsDto(
            total,
            published,
            draft,
            trash,
            totalLikes,
            totalDislikes,
            totalViews,
            Math.Round(averageReading, 1));
    }

    private sealed record StatisticsProjection(
        BlogStatus Status,
        int LikeCount,
        int DislikeCount,
        int ReadingTimeMinutes,
        int ViewCount);
}

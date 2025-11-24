using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Arsis.Domain.Entities.Blogs;
using Arsis.Domain.Enums;
using Arsis.Infrastructure.Persistence;
using EndPoint.WebSite.Models.Blog;
using Microsoft.EntityFrameworkCore;

using BlogEntity = Arsis.Domain.Entities.Blogs.Blog;

namespace EndPoint.WebSite.Services.Blog;

public sealed class DatabaseBlogService : IBlogService
{
    private static readonly string DefaultHeroImage = "https://placehold.co/1600x900?text=Arsis+Blog";

    private readonly AppDbContext _dbContext;

    public DatabaseBlogService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<BlogPost>> GetAllPostsAsync(CancellationToken cancellationToken = default)
    {
        var blogs = await QueryPublishedPosts()
            .OrderByDescending(blog => blog.PublishedAt ?? blog.UpdateDate)
            .ThenByDescending(blog => blog.CreateDate)
            .ToListAsync(cancellationToken);

        return blogs.Select(MapToBlogPost).ToList();
    }

    public async Task<BlogPost?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return null;
        }

        var normalizedSlug = slug.Trim();

        var blog = await QueryPublishedPosts()
            .Include(entity => entity.Views)
            .Include(entity => entity.Comments.Where(comment => !comment.IsDeleted && comment.IsApproved))
            .FirstOrDefaultAsync(entity => entity.SeoSlug == normalizedSlug, cancellationToken);

        return blog is null ? null : MapToBlogPost(blog);
    }

    public async Task<IReadOnlyList<BlogPost>> GetLatestPostsAsync(int count, CancellationToken cancellationToken = default)
    {
        var take = Math.Max(1, count);

        var blogs = await QueryPublishedPosts()
            .OrderByDescending(blog => blog.PublishedAt ?? blog.UpdateDate)
            .ThenByDescending(blog => blog.CreateDate)
            .Take(take)
            .ToListAsync(cancellationToken);

        return blogs.Select(MapToBlogPost).ToList();
    }

    public async Task<IReadOnlyList<BlogCommentViewModel>> GetCommentsAsync(Guid blogId, CancellationToken cancellationToken = default)
    {
        if (blogId == Guid.Empty)
        {
            return Array.Empty<BlogCommentViewModel>();
        }

        var comments = await _dbContext.BlogComments
            .AsNoTracking()
            .Where(comment => comment.BlogId == blogId && !comment.IsDeleted && comment.IsApproved)
            .OrderBy(comment => comment.CreateDate)
            .ToListAsync(cancellationToken);

        if (comments.Count == 0)
        {
            return Array.Empty<BlogCommentViewModel>();
        }

        var lookup = comments
            .GroupBy(comment => comment.ParentId ?? Guid.Empty)
            .ToDictionary(group => group.Key, group => group.ToList());

        List<BlogCommentViewModel> BuildTree(Guid parentKey)
        {
            if (!lookup.TryGetValue(parentKey, out var children))
            {
                return new List<BlogCommentViewModel>();
            }

            return children
                .Select(child => new BlogCommentViewModel
                {
                    Id = child.Id,
                    ParentId = child.ParentId,
                    AuthorName = child.AuthorName,
                    Content = child.Content,
                    CreatedAt = child.CreateDate,
                    Replies = BuildTree(child.Id)
                })
                .ToList();
        }

        return BuildTree(Guid.Empty);
    }

    public async Task<bool> AddCommentAsync(Guid blogId, string authorName, string content, string? authorEmail, Guid? parentId, CancellationToken cancellationToken = default)
    {
        if (blogId == Guid.Empty)
        {
            return false;
        }

        var blogExists = await _dbContext.Blogs
            .AsNoTracking()
            .AnyAsync(entity => entity.Id == blogId && !entity.IsDeleted && entity.Status == BlogStatus.Published, cancellationToken);

        if (!blogExists)
        {
            return false;
        }

        BlogComment? parentComment = null;
        if (parentId.HasValue)
        {
            parentComment = await _dbContext.BlogComments
                .FirstOrDefaultAsync(
                    comment => comment.Id == parentId.Value && comment.BlogId == blogId && !comment.IsDeleted,
                    cancellationToken);

            if (parentComment is null)
            {
                return false;
            }
        }

        var comment = new BlogComment(blogId, authorName, content, authorEmail, parentComment);

        _dbContext.BlogComments.Add(comment);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            _dbContext.ChangeTracker.Clear();
            return false;
        }

        await _dbContext.Blogs
            .Where(blog => blog.Id == blogId)
            .ExecuteUpdateAsync(setters =>
                setters.SetProperty(blog => blog.UpdateDate, _ => DateTimeOffset.UtcNow), cancellationToken);

        return true;
    }

    public async Task<int> RegisterViewAsync(Guid blogId, IPAddress? viewerIp, CancellationToken cancellationToken = default)
    {
        if (blogId == Guid.Empty)
        {
            return 0;
        }

        var blogExists = await _dbContext.Blogs
            .AsNoTracking()
            .AnyAsync(entity => entity.Id == blogId && !entity.IsDeleted && entity.Status == BlogStatus.Published, cancellationToken);

        if (!blogExists)
        {
            return 0;
        }

        var address = viewerIp ?? IPAddress.None;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var existingView = await _dbContext.BlogViews
            .FirstOrDefaultAsync(
                view => view.BlogId == blogId && view.ViewerIp == address && view.ViewDate == today,
                cancellationToken);

        if (existingView is null)
        {
            _dbContext.BlogViews.Add(new BlogDailyView(blogId, address, today));
        }
        else
        {
            existingView.UpdateViewDate(today);
        }

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            _dbContext.ChangeTracker.Clear();
        }

        return await _dbContext.BlogViews
            .AsNoTracking()
            .CountAsync(view => view.BlogId == blogId, cancellationToken);
    }

    private IQueryable<BlogEntity> QueryPublishedPosts()
        => _dbContext.Blogs
            .AsNoTracking()
            .Include(blog => blog.Author)
            .Where(blog => !blog.IsDeleted && blog.Status == BlogStatus.Published);

    private static BlogPost MapToBlogPost(BlogEntity blog)
    {
        var publishedAt = blog.PublishedAt?.LocalDateTime ?? blog.UpdateDate.LocalDateTime;
        var heroImageUrl = ResolveHeroImageUrl(blog.FeaturedImagePath);
        var sections = BuildContentSections(blog.Content);
        var tags = blog.Tags?.ToArray() ?? Array.Empty<string>();

        return new BlogPost
        {
            Id = blog.Id,
            Slug = blog.SeoSlug,
            Title = blog.Title,
            Summary = blog.Summary,
            HeroImageUrl = heroImageUrl,
            AuthorName = blog.Author?.DisplayName ?? "آرسیس",
            AuthorRole = blog.Author?.Bio,
            PublishedAt = publishedAt,
            ReadingTimeMinutes = Math.Max(1, blog.ReadingTimeMinutes),
            Tags = tags,
            ContentSections = sections,
            ContentHtml = sections.Count > 0 ? null : blog.Content,
            FeaturedQuote = null,
            KeyInsights = Array.Empty<string>(),
            SeoTitle = string.IsNullOrWhiteSpace(blog.SeoTitle) ? blog.Title : blog.SeoTitle,
            SeoDescription = blog.SeoDescription,
            SeoKeywords = blog.SeoKeywords,
            RobotsDirective = string.IsNullOrWhiteSpace(blog.Robots) ? "index,follow" : blog.Robots,
            TotalViews = blog.Views?.Count ?? 0,
            CommentCount = blog.Comments?.Count(comment => comment.IsApproved && !comment.IsDeleted) ?? 0
        };
    }

    private static IReadOnlyList<BlogContentSection> BuildContentSections(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return Array.Empty<BlogContentSection>();
        }

        if (content.IndexOf('<') >= 0)
        {
            return Array.Empty<BlogContentSection>();
        }

        var paragraphs = content
            .Split(new[] { "\r\n\r\n", "\n\n" }, StringSplitOptions.RemoveEmptyEntries)
            .Select(paragraph => paragraph.Trim())
            .Where(paragraph => paragraph.Length > 0)
            .ToArray();

        if (paragraphs.Length == 0)
        {
            return Array.Empty<BlogContentSection>();
        }

        return paragraphs
            .Select(paragraph => new BlogContentSection
            {
                Body = paragraph,
                IsHighlighted = false
            })
            .ToList();
    }

    private static string ResolveHeroImageUrl(string? imagePath)
    {
        if (string.IsNullOrWhiteSpace(imagePath))
        {
            return DefaultHeroImage;
        }

        var trimmed = imagePath.Trim();

        if (trimmed.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            return trimmed;
        }

        if (trimmed.StartsWith("~/", StringComparison.Ordinal))
        {
            return trimmed[1..];
        }

        return trimmed.StartsWith('/') ? trimmed : "/" + trimmed;
    }
}

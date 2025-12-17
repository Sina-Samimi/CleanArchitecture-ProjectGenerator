using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LogTableRenameTest.Infrastructure.Persistence;
using LogTableRenameTest.WebSite.Models.Product;
using Microsoft.EntityFrameworkCore;

using ProductCommentEntity = LogTableRenameTest.Domain.Entities.Catalog.ProductComment;
using ProductEntity = LogTableRenameTest.Domain.Entities.Catalog.Product;

namespace LogTableRenameTest.WebSite.Services.Products;

public sealed class DatabaseProductCatalogService : IProductCatalogService
{
    private const string DefaultProductImage = "https://placehold.co/800x600?text=Arsis+Product";

    private readonly AppDbContext _dbContext;

    public DatabaseProductCatalogService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<Product>> GetFeaturedProductsAsync(int count, CancellationToken cancellationToken = default)
    {
        var take = Math.Max(1, count);

        var products = await QueryPublishedProducts()
            .OrderByDescending(product => product.PublishedAt ?? product.UpdateDate)
            .ThenBy(product => product.Name)
            .Take(take)
            .ToListAsync(cancellationToken);

        return products
            .Select(product => MapToProduct(product, includeFaqs: false, includeExecutionSteps: false, includeComments: false))
            .ToList();
    }

    public async Task<ProductListResult> GetProductsAsync(
        ProductFilterOptions filterOptions,
        CancellationToken cancellationToken = default)
    {
        var publishedQuery = QueryPublishedProducts();

        var prices = await publishedQuery
            .Where(product => product.Price.HasValue)
            .Select(product => product.Price!.Value)
            .ToListAsync(cancellationToken);

        decimal priceRangeMin = 0;
        decimal priceRangeMax = 0;
        if (prices.Count > 0)
        {
            priceRangeMin = prices.Min();
            priceRangeMax = prices.Max();
        }

        var categories = await publishedQuery
            .Where(product => product.Category != null && !string.IsNullOrWhiteSpace(product.Category.Name))
            .Select(product => product.Category!.Name)
            .Distinct()
            .OrderBy(name => name)
            .ToListAsync(cancellationToken);

        var filteredQuery = ApplyFilters(publishedQuery, filterOptions);

        var orderedQuery = ApplyOrdering(filteredQuery, filterOptions.SortBy);

        var filteredProducts = await orderedQuery
            .ToListAsync(cancellationToken);

        var productModels = filteredProducts
            .Select(product => MapToProduct(product, includeFaqs: false, includeExecutionSteps: false, includeComments: false))
            .ToList();

        return new ProductListResult(
            productModels,
            productModels.Count,
            categories,
            Array.Empty<string>(),
            priceRangeMin,
            priceRangeMax);
    }

    public async Task<Product?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return null;
        }

        var normalizedSlug = slug.Trim();

        var product = await QueryPublishedProducts()
            .Include(entity => entity.ExecutionSteps)
            .Include(entity => entity.Faqs)
            .Include(entity => entity.Attributes)
            .Include(entity => entity.Gallery)
            .Include(entity => entity.Comments.Where(comment => !comment.IsDeleted && comment.IsApproved))
            .FirstOrDefaultAsync(entity => entity.SeoSlug == normalizedSlug, cancellationToken);

        return product is null
            ? null
            : MapToProduct(product, includeFaqs: true, includeExecutionSteps: true, includeComments: true);
    }

    public async Task<IReadOnlyList<Product>> GetRelatedProductsAsync(
        Guid productId,
        int count,
        CancellationToken cancellationToken = default)
    {
        if (productId == Guid.Empty)
        {
            return Array.Empty<Product>();
        }

        var currentProduct = await QueryPublishedProducts()
            .FirstOrDefaultAsync(product => product.Id == productId, cancellationToken);

        if (currentProduct is null)
        {
            return Array.Empty<Product>();
        }

        var take = Math.Max(1, count);

        var relatedProducts = await QueryPublishedProducts()
            .Where(product => product.Id != productId && product.CategoryId == currentProduct.CategoryId)
            .OrderByDescending(product => product.PublishedAt ?? product.UpdateDate)
            .ThenBy(product => product.Name)
            .Take(take)
            .ToListAsync(cancellationToken);

        if (relatedProducts.Count < take)
        {
            var fallbackProducts = await QueryPublishedProducts()
                .Where(product => product.Id != productId && product.CategoryId != currentProduct.CategoryId)
                .OrderByDescending(product => product.PublishedAt ?? product.UpdateDate)
                .ThenBy(product => product.Name)
                .Take(take - relatedProducts.Count)
                .ToListAsync(cancellationToken);

            relatedProducts.AddRange(fallbackProducts);
        }

        return relatedProducts
            .Select(product => MapToProduct(product, includeFaqs: false, includeExecutionSteps: false, includeComments: false))
            .ToList();
    }

    public async Task<bool> AddCommentAsync(
        Guid productId,
        string authorName,
        string content,
        double rating,
        Guid? parentId = null,
        CancellationToken cancellationToken = default)
    {
        if (productId == Guid.Empty)
        {
            return false;
        }

        var trimmedAuthor = string.IsNullOrWhiteSpace(authorName) ? "کاربر مهمان" : authorName.Trim();
        var trimmedContent = content?.Trim();
        if (string.IsNullOrWhiteSpace(trimmedContent))
        {
            return false;
        }

        var productExists = await _dbContext.Products
            .AsNoTracking()
            .AnyAsync(entity => entity.Id == productId && !entity.IsDeleted && entity.IsPublished, cancellationToken);

        if (!productExists)
        {
            return false;
        }

        ProductCommentEntity? parentComment = null;

        if (parentId.HasValue)
        {
            parentComment = await _dbContext.ProductComments
                .FirstOrDefaultAsync(
                    comment => comment.Id == parentId.Value && comment.ProductId == productId && !comment.IsDeleted,
                    cancellationToken);

            if (parentComment is null)
            {
                return false;
            }
        }

        var comment = new ProductCommentEntity(
            productId,
            trimmedAuthor,
            trimmedContent,
            rating,
            parentComment,
            isApproved: false);

        _dbContext.ProductComments.Add(comment);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            _dbContext.ChangeTracker.Clear();
            return false;
        }
        catch (DbUpdateException)
        {
            _dbContext.ChangeTracker.Clear();
            return false;
        }

        await _dbContext.Products
            .Where(product => product.Id == productId)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(product => product.UpdateDate, _ => DateTimeOffset.UtcNow),
                cancellationToken);

        return true;
    }

    private IQueryable<ProductEntity> QueryPublishedProducts()
        => _dbContext.Products
            .AsNoTracking()
            .Include(product => product.Category)
            .Where(product => !product.IsDeleted && product.IsPublished);

    private static IQueryable<ProductEntity> ApplyFilters(
        IQueryable<ProductEntity> query,
        ProductFilterOptions filterOptions)
    {
        if (!string.IsNullOrWhiteSpace(filterOptions.SearchTerm))
        {
            var search = filterOptions.SearchTerm.Trim();
            query = query.Where(product =>
                product.Name.Contains(search) ||
                product.Summary.Contains(search) ||
                product.Description.Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(filterOptions.Category))
        {
            var category = filterOptions.Category.Trim();
            query = query.Where(product => product.Category != null && product.Category.Name == category);
        }

        if (filterOptions.MinPrice.HasValue)
        {
            query = query.Where(product => product.Price.HasValue && product.Price.Value >= filterOptions.MinPrice.Value);
        }

        if (filterOptions.MaxPrice.HasValue)
        {
            query = query.Where(product => product.Price.HasValue && product.Price.Value <= filterOptions.MaxPrice.Value);
        }

        return query;
    }

    private static IQueryable<ProductEntity> ApplyOrdering(
        IQueryable<ProductEntity> query,
        string? sortBy)
    {
        return sortBy switch
        {
            "price-asc" => query
                .OrderBy(product => product.Price ?? decimal.MaxValue)
                .ThenBy(product => product.Name),
            "price-desc" => query
                .OrderByDescending(product => product.Price ?? decimal.MinValue)
                .ThenBy(product => product.Name),
            _ => query
                .OrderByDescending(product => product.PublishedAt ?? product.UpdateDate)
                .ThenBy(product => product.Name)
        };
    }

    private static Product MapToProduct(
        ProductEntity product,
        bool includeFaqs,
        bool includeExecutionSteps,
        bool includeComments)
    {
        var imageUrl = ResolveImageUrl(product.FeaturedImagePath);
        var publishedAt = product.PublishedAt ?? product.UpdateDate;
        var tags = product.Tags?.ToArray() ?? Array.Empty<string>();

        IReadOnlyList<ProductFaqItem> faqItems = Array.Empty<ProductFaqItem>();
        if (includeFaqs && product.Faqs is not null)
        {
            faqItems = product.Faqs
                .Where(faq => !faq.IsDeleted)
                .OrderBy(faq => faq.DisplayOrder)
                .Select(faq => new ProductFaqItem
                {
                    Question = faq.Question,
                    Answer = faq.Answer
                })
                .ToList();
        }

        IReadOnlyList<ProductModule> modules = Array.Empty<ProductModule>();
        if (includeExecutionSteps && product.ExecutionSteps is not null)
        {
            modules = product.ExecutionSteps
                .Where(step => !step.IsDeleted)
                .OrderBy(step => step.DisplayOrder)
                .ThenBy(step => step.CreateDate)
                .Select(step => new ProductModule
                {
                    Title = step.Title,
                    Description = step.Description,
                    Duration = step.Duration
                })
                .ToList();
        }

        IReadOnlyList<ProductAttribute> attributes = Array.Empty<ProductAttribute>();
        if (product.Attributes is not null)
        {
            attributes = product.Attributes
                .Where(attr => !attr.IsDeleted)
                .OrderBy(attr => attr.DisplayOrder)
                .ThenBy(attr => attr.CreateDate)
                .Select(attr => new ProductAttribute
                {
                    Key = attr.Key,
                    Value = attr.Value
                })
                .ToList();
        }

        IReadOnlyList<ProductComment> comments = Array.Empty<ProductComment>();
        double averageRating = 0;
        if (includeComments && product.Comments is not null)
        {
            comments = product.Comments
                .Where(comment => !comment.IsDeleted && comment.IsApproved)
                .OrderByDescending(comment => comment.CreateDate)
                .Select(comment => new ProductComment
                {
                    Id = comment.Id,
                    ParentId = comment.ParentId,
                    AuthorName = comment.AuthorName,
                    Content = comment.Content,
                    CreatedAt = comment.CreateDate,
                    Rating = comment.Rating
                })
                .ToList();

            if (comments.Count > 0)
            {
                averageRating = Math.Round(comments.Average(comment => comment.Rating), 1, MidpointRounding.AwayFromZero);
            }
        }

        IReadOnlyList<ProductGalleryImage> gallery = Array.Empty<ProductGalleryImage>();
        if (product.Gallery is not null)
        {
            gallery = product.Gallery
                .OrderBy(image => image.DisplayOrder)
                .ThenBy(image => image.CreateDate)
                .Select(image => new ProductGalleryImage
                {
                    Id = image.Id,
                    ImagePath = ResolveImageUrl(image.ImagePath),
                    DisplayOrder = image.DisplayOrder
                })
                .ToList();
        }

        return new Product
        {
            Id = product.Id,
            Slug = string.IsNullOrWhiteSpace(product.SeoSlug) ? product.Id.ToString("N") : product.SeoSlug!,
            Name = product.Name,
            ShortDescription = product.Summary,
            Description = product.Description,
            HeroImageUrl = imageUrl,
            ThumbnailUrl = imageUrl,
            Price = product.Price,
            OriginalPrice = product.CompareAtPrice,
            IsCustomOrder = product.IsCustomOrder,
            Rating = averageRating,
            ReviewCount = comments.Count,
            DifficultyLevel = null,
            Category = product.Category?.Name,
            DeliveryFormat = null,
            Duration = null,
            PublishedAt = publishedAt,
            Tags = tags,
            Highlights = Array.Empty<string>(),
            Modules = modules,
            Statistics = Array.Empty<ProductStatistic>(),
            FaqItems = faqItems,
            Attributes = attributes,
            Comments = comments,
            Gallery = gallery,
            IsFeatured = product.IsPublished
        };
    }

    private static string ResolveImageUrl(string? imagePath)
    {
        if (string.IsNullOrWhiteSpace(imagePath))
        {
            return DefaultProductImage;
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

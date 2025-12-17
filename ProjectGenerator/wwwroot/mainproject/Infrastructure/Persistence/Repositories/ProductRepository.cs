using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Attar.Application.DTOs.Catalog;
using Attar.Application.Interfaces;
using Attar.Domain.Entities.Catalog;
using Microsoft.EntityFrameworkCore;

namespace Attar.Infrastructure.Persistence.Repositories;

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
            .Include(product => product.VariantAttributes)
            .Include(product => product.Variants)
                .ThenInclude(variant => variant.Options)
            .FirstOrDefaultAsync(product => product.Id == id && !product.IsDeleted, cancellationToken);

    public async Task<Product?> GetBySlugAsync(string slug, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return null;
        }

        var normalizedSlug = slug.Trim();
        return await _dbContext.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(product => !product.IsDeleted && product.SeoSlug == normalizedSlug, cancellationToken);
    }

    public async Task<Product?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken)
        => await _dbContext.Products
            .Include(product => product.Category)
            .Include(product => product.Gallery)
            .Include(product => product.ExecutionSteps)
            .Include(product => product.Faqs)
            .Include(product => product.Attributes)
            .Include(product => product.VariantAttributes)
            .Include(product => product.Variants)
                .ThenInclude(variant => variant.Options)
            .Include(product => product.Comments.Where(c => !c.IsDeleted))
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

        var totalPages = filteredCount == 0 ? 1 : (int)Math.Ceiling(filteredCount / (double)pageSize);
        var pageNumber = page > totalPages ? totalPages : page;
        if (pageNumber <= 0)
        {
            pageNumber = 1;
        }

        var skip = (pageNumber - 1) * pageSize;
        if (skip < 0)
        {
            skip = 0;
        }

        var orderedQuery = filteredQuery
            .OrderByDescending(product => product.UpdateDate)
            .ThenBy(product => product.Name);

        var productIds = await orderedQuery
            .Skip(skip)
            .Take(pageSize)
            .Select(product => product.Id)
            .ToListAsync(cancellationToken);

        if (productIds.Count == 0)
        {
            return new ProductListResultDto(
                Array.Empty<ProductListItemDto>(),
                totalCount,
                filteredCount,
                pageNumber,
                pageSize,
                totalPages);
        }

        // Get seller counts for products (count distinct sellers from offers)
        var offerSellerData = await _dbContext.ProductOffers
            .AsNoTracking()
            .Where(offer => productIds.Contains(offer.ProductId) && !offer.IsDeleted && !string.IsNullOrWhiteSpace(offer.SellerId))
            .GroupBy(offer => offer.ProductId)
            .Select(g => new { ProductId = g.Key, SellerIds = g.Select(o => o.SellerId!).Distinct().ToList() })
            .ToListAsync(cancellationToken);

        var sellerCounts = offerSellerData
            .ToDictionary(x => x.ProductId, x => x.SellerIds.Count);
        
        var offerSellerLookup = offerSellerData
            .ToDictionary(x => x.ProductId, x => x.SellerIds);

        // Get products in memory first
        var productsList = await orderedQuery
            .Where(product => productIds.Contains(product.Id))
            .Select(product => new
            {
                product.Id,
                product.Name,
                product.Type,
                product.Price,
                product.CompareAtPrice,
                product.IsPublished,
                product.PublishedAt,
                product.CategoryId,
                CategoryName = product.Category.Name,
                product.FeaturedImagePath,
                TagList = product.TagList ?? string.Empty,
                product.UpdateDate,
                product.IsCustomOrder,
                product.SellerId
            })
            .ToListAsync(cancellationToken);

        // Create order lookup
        var orderLookup = productIds
            .Select((id, index) => new { Id = id, Order = index })
            .ToDictionary(x => x.Id, x => x.Order);

        // Get seller names and phones for main sellers
        var mainSellerIds = productsList
            .Where(p => !string.IsNullOrWhiteSpace(p.SellerId))
            .Select(p => p.SellerId!)
            .Distinct()
            .ToList();

        var mainSellerInfoList = mainSellerIds.Count > 0
            ? await _dbContext.SellerProfiles
                .AsNoTracking()
                .Where(s => mainSellerIds.Contains(s.UserId) && !s.IsDeleted)
                .Select(s => new { s.UserId, s.DisplayName, s.ContactPhone })
                .ToListAsync(cancellationToken)
            : (await _dbContext.SellerProfiles
                .AsNoTracking()
                .Where(s => false)
                .Select(s => new { s.UserId, s.DisplayName, s.ContactPhone })
                .ToListAsync(cancellationToken));

        var mainSellerInfo = mainSellerInfoList
            .ToDictionary(s => s.UserId, s => new { s.DisplayName, s.ContactPhone });

        // Sort by original order and map to DTOs
        var items = productsList
            .OrderBy(p => orderLookup.GetValueOrDefault(p.Id, int.MaxValue))
            .Select(product =>
            {
                var sellerInfo = !string.IsNullOrWhiteSpace(product.SellerId) && mainSellerInfo.TryGetValue(product.SellerId, out var info)
                    ? info
                    : null;

                // Calculate seller count: count offers + main seller (if not already in offers) + site (if SellerId is null)
                var offerCount = sellerCounts.ContainsKey(product.Id) ? sellerCounts[product.Id] : 0;
                var mainSellerInOffers = !string.IsNullOrWhiteSpace(product.SellerId) && 
                    offerSellerLookup.ContainsKey(product.Id) &&
                    offerSellerLookup[product.Id].Contains(product.SellerId);
                
                // Count main seller if it exists in SellerProfiles (real seller, not site)
                var hasMainSeller = !string.IsNullOrWhiteSpace(product.SellerId) && 
                    mainSellerInfo.ContainsKey(product.SellerId);
                var mainSellerCount = hasMainSeller && !mainSellerInOffers ? 1 : 0;
                
                // Count site as a seller if Product.SellerId is null (product belongs to site)
                var siteCount = string.IsNullOrWhiteSpace(product.SellerId) ? 1 : 0;
                
                var sellerCount = offerCount + mainSellerCount + siteCount;

                return new ProductListItemDto(
                    product.Id,
                    product.Name,
                    product.Type,
                    product.Price,
                    product.CompareAtPrice,
                    product.IsPublished,
                    product.PublishedAt,
                    product.CategoryId,
                    product.CategoryName,
                    product.FeaturedImagePath,
                    product.TagList,
                    product.UpdateDate,
                    product.IsCustomOrder,
                    product.SellerId,
                    sellerInfo?.DisplayName,
                    sellerInfo?.ContactPhone,
                    sellerCount);
            })
            .ToList();

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
            .Include(product => product.VariantAttributes)
            .Include(product => product.Variants)
                .ThenInclude(variant => variant.Options)
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

    public async Task<IReadOnlyCollection<ProductListItemDto>> GetBySellerAsync(string sellerId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(sellerId))
        {
            return Array.Empty<ProductListItemDto>();
        }

        var normalizedSellerId = sellerId.Trim();

        var items = await _dbContext.Products
            .AsNoTracking()
            .Where(product => !product.IsDeleted
                && (product.CreatorId == normalizedSellerId || product.SellerId == normalizedSellerId))
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
                product.UpdateDate,
                product.IsCustomOrder,
                product.SellerId,
                product.SellerId != null ? _dbContext.SellerProfiles
                    .Where(s => s.UserId == product.SellerId && !s.IsDeleted)
                    .Select(s => s.DisplayName)
                    .FirstOrDefault() : null,
                product.SellerId != null ? _dbContext.SellerProfiles
                    .Where(s => s.UserId == product.SellerId && !s.IsDeleted)
                    .Select(s => s.ContactPhone)
                    .FirstOrDefault() : null,
                0)) // SellerCount will be calculated separately if needed
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
        else if (entry.State == EntityState.Unchanged)
        {
            // Mark as modified to ensure changes are saved
            entry.State = EntityState.Modified;
        }
        // If already Modified or Added, no need to change state

        await SyncExecutionStepsAsync(product, cancellationToken);
        await SyncFaqsAsync(product, cancellationToken);
        await SyncAttributesAsync(product, cancellationToken);
        await SyncVariantAttributesAsync(product, cancellationToken);
        await SyncVariantsAsync(product, cancellationToken);

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

    private async Task SyncAttributesAsync(Product product, CancellationToken cancellationToken)
    {
        if (product.Attributes.Count == 0)
        {
            return;
        }

        var existingIds = await _dbContext.ProductAttributes
            .AsNoTracking()
            .Where(attr => attr.ProductId == product.Id)
            .Select(attr => attr.Id)
            .ToListAsync(cancellationToken);

        var existingLookup = existingIds.Count == 0 ? null : existingIds.ToHashSet();

        foreach (var attribute in product.Attributes)
        {
            var attributeEntry = _dbContext.Entry(attribute);

            if (attributeEntry.State == EntityState.Detached)
            {
                if (existingLookup is not null && existingLookup.Contains(attribute.Id))
                {
                    _dbContext.ProductAttributes.Attach(attribute);
                    attributeEntry.State = EntityState.Modified;
                }
                else
                {
                    _dbContext.ProductAttributes.Add(attribute);
                }

                continue;
            }

            var existsInDatabase = existingLookup is not null && existingLookup.Contains(attribute.Id);

            if (!existsInDatabase)
            {
                attributeEntry.State = EntityState.Added;
            }
        }
    }

    private async Task SyncVariantAttributesAsync(Product product, CancellationToken cancellationToken)
    {
        var existingIds = await _dbContext.ProductVariantAttributes
            .AsNoTracking()
            .Where(attr => attr.ProductId == product.Id)
            .Select(attr => attr.Id)
            .ToListAsync(cancellationToken);

        var existingLookup = existingIds.Count == 0 ? null : existingIds.ToHashSet();

        // Remove attributes that are no longer in the collection
        if (existingLookup is not null)
        {
            var currentIds = product.VariantAttributes.Select(a => a.Id).ToHashSet();
            var toRemove = existingLookup.Except(currentIds).ToList();
            if (toRemove.Count > 0)
            {
                var toRemoveEntities = await _dbContext.ProductVariantAttributes
                    .Where(attr => toRemove.Contains(attr.Id))
                    .ToListAsync(cancellationToken);
                _dbContext.ProductVariantAttributes.RemoveRange(toRemoveEntities);
            }
        }

        foreach (var attribute in product.VariantAttributes)
        {
            var attributeEntry = _dbContext.Entry(attribute);

            if (attributeEntry.State == EntityState.Detached)
            {
                if (existingLookup is not null && existingLookup.Contains(attribute.Id))
                {
                    _dbContext.ProductVariantAttributes.Attach(attribute);
                    attributeEntry.State = EntityState.Modified;
                }
                else
                {
                    _dbContext.ProductVariantAttributes.Add(attribute);
                }

                continue;
            }

            var existsInDatabase = existingLookup is not null && existingLookup.Contains(attribute.Id);

            if (!existsInDatabase)
            {
                attributeEntry.State = EntityState.Added;
            }
        }
    }

    private async Task SyncVariantsAsync(Product product, CancellationToken cancellationToken)
    {
        var existingIds = await _dbContext.ProductVariants
            .AsNoTracking()
            .Where(v => v.ProductId == product.Id)
            .Select(v => v.Id)
            .ToListAsync(cancellationToken);

        var existingLookup = existingIds.Count == 0 ? null : existingIds.ToHashSet();

        // Remove variants that are no longer in the collection
        if (existingLookup is not null)
        {
            var currentIds = product.Variants.Select(v => v.Id).ToHashSet();
            var toRemove = existingLookup.Except(currentIds).ToList();
            if (toRemove.Count > 0)
            {
                var toRemoveEntities = await _dbContext.ProductVariants
                    .Where(v => toRemove.Contains(v.Id))
                    .ToListAsync(cancellationToken);
                _dbContext.ProductVariants.RemoveRange(toRemoveEntities);
            }
        }

        foreach (var variant in product.Variants)
        {
            var variantEntry = _dbContext.Entry(variant);

            if (variantEntry.State == EntityState.Detached)
            {
                if (existingLookup is not null && existingLookup.Contains(variant.Id))
                {
                    _dbContext.ProductVariants.Attach(variant);
                    variantEntry.State = EntityState.Modified;
                }
                else
                {
                    _dbContext.ProductVariants.Add(variant);
                }

                // Sync variant options
                await SyncVariantOptionsAsync(variant, cancellationToken);
                continue;
            }

            var existsInDatabase = existingLookup is not null && existingLookup.Contains(variant.Id);

            if (!existsInDatabase)
            {
                variantEntry.State = EntityState.Added;
            }

            // Sync variant options
            await SyncVariantOptionsAsync(variant, cancellationToken);
        }
    }

    private async Task SyncVariantOptionsAsync(ProductVariant variant, CancellationToken cancellationToken)
    {
        var existingIds = await _dbContext.ProductVariantOptions
            .AsNoTracking()
            .Where(opt => opt.VariantId == variant.Id)
            .Select(opt => opt.Id)
            .ToListAsync(cancellationToken);

        var existingLookup = existingIds.Count == 0 ? null : existingIds.ToHashSet();

        // Remove options that are no longer in the collection
        if (existingLookup is not null)
        {
            var currentIds = variant.Options.Select(o => o.Id).ToHashSet();
            var toRemove = existingLookup.Except(currentIds).ToList();
            if (toRemove.Count > 0)
            {
                var toRemoveEntities = await _dbContext.ProductVariantOptions
                    .Where(opt => toRemove.Contains(opt.Id))
                    .ToListAsync(cancellationToken);
                _dbContext.ProductVariantOptions.RemoveRange(toRemoveEntities);
            }
        }

        foreach (var option in variant.Options)
        {
            var optionEntry = _dbContext.Entry(option);

            if (optionEntry.State == EntityState.Detached)
            {
                if (existingLookup is not null && existingLookup.Contains(option.Id))
                {
                    _dbContext.ProductVariantOptions.Attach(option);
                    optionEntry.State = EntityState.Modified;
                }
                else
                {
                    _dbContext.ProductVariantOptions.Add(option);
                }

                continue;
            }

            var existsInDatabase = existingLookup is not null && existingLookup.Contains(option.Id);

            if (!existsInDatabase)
            {
                optionEntry.State = EntityState.Added;
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

        if (!string.IsNullOrWhiteSpace(filter.SellerId))
        {
            var sellerId = filter.SellerId.Trim();
            query = query.Where(product => product.SellerId == sellerId);
        }

        return query;
    }
}

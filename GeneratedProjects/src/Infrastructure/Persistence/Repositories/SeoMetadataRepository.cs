using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TestAttarClone.Application.Interfaces;
using TestAttarClone.Domain.Entities.Seo;
using TestAttarClone.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace TestAttarClone.Infrastructure.Persistence.Repositories;

public sealed class SeoMetadataRepository : ISeoMetadataRepository
{
    private readonly AppDbContext _dbContext;

    public SeoMetadataRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<SeoMetadata?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => await _dbContext.SeoMetadata
            .AsNoTracking()
            .FirstOrDefaultAsync(seo => seo.Id == id, cancellationToken);

    public async Task<SeoMetadata?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken)
        => await _dbContext.SeoMetadata
            .FirstOrDefaultAsync(seo => seo.Id == id, cancellationToken);

    public async Task<SeoMetadata?> GetByPageTypeAndIdentifierAsync(SeoPageType pageType, string? pageIdentifier, CancellationToken cancellationToken)
    {
        var query = _dbContext.SeoMetadata
            .AsNoTracking()
            .Where(seo => seo.PageType == pageType);

        if (string.IsNullOrWhiteSpace(pageIdentifier))
        {
            query = query.Where(seo => seo.PageIdentifier == null);
        }
        else
        {
            query = query.Where(seo => seo.PageIdentifier == pageIdentifier);
        }

        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<SeoMetadata>> GetAllAsync(CancellationToken cancellationToken)
        => await _dbContext.SeoMetadata
            .AsNoTracking()
            .OrderBy(seo => seo.PageType)
            .ThenBy(seo => seo.PageIdentifier ?? string.Empty)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyCollection<SeoMetadata>> GetByPageTypeAsync(SeoPageType pageType, CancellationToken cancellationToken)
        => await _dbContext.SeoMetadata
            .AsNoTracking()
            .Where(seo => seo.PageType == pageType)
            .OrderBy(seo => seo.PageIdentifier ?? string.Empty)
            .ToListAsync(cancellationToken);

    public async Task<bool> ExistsAsync(SeoPageType pageType, string? pageIdentifier, CancellationToken cancellationToken)
    {
        var query = _dbContext.SeoMetadata
            .Where(seo => seo.PageType == pageType);

        if (string.IsNullOrWhiteSpace(pageIdentifier))
        {
            query = query.Where(seo => seo.PageIdentifier == null);
        }
        else
        {
            query = query.Where(seo => seo.PageIdentifier == pageIdentifier);
        }

        return await query.AnyAsync(cancellationToken);
    }

    public async Task AddAsync(SeoMetadata seoMetadata, CancellationToken cancellationToken)
    {
        await _dbContext.SeoMetadata.AddAsync(seoMetadata, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(SeoMetadata seoMetadata, CancellationToken cancellationToken)
    {
        var existing = await _dbContext.SeoMetadata
            .FirstOrDefaultAsync(seo => seo.Id == seoMetadata.Id, cancellationToken);

        if (existing is null)
        {
            return;
        }

        _dbContext.Entry(existing).CurrentValues.SetValues(seoMetadata);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(SeoMetadata seoMetadata, CancellationToken cancellationToken)
    {
        _dbContext.SeoMetadata.Remove(seoMetadata);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}


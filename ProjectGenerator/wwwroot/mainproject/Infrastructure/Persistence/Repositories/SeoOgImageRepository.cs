using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MobiRooz.Application.Interfaces;
using MobiRooz.Domain.Entities.Seo;
using Microsoft.EntityFrameworkCore;

namespace MobiRooz.Infrastructure.Persistence.Repositories;

public sealed class SeoOgImageRepository : ISeoOgImageRepository
{
    private readonly AppDbContext _dbContext;

    public SeoOgImageRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<SeoOgImage?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => await _dbContext.SeoOgImages
            .AsNoTracking()
            .FirstOrDefaultAsync(image => image.Id == id, cancellationToken);

    public async Task<IReadOnlyCollection<SeoOgImage>> GetBySeoMetadataIdAsync(Guid seoMetadataId, CancellationToken cancellationToken)
        => await _dbContext.SeoOgImages
            .AsNoTracking()
            .Where(image => image.SeoMetadataId == seoMetadataId)
            .OrderBy(image => image.DisplayOrder)
            .ThenBy(image => image.CreateDate)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(SeoOgImage image, CancellationToken cancellationToken)
    {
        await _dbContext.SeoOgImages.AddAsync(image, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(SeoOgImage image, CancellationToken cancellationToken)
    {
        var existing = await _dbContext.SeoOgImages
            .FirstOrDefaultAsync(i => i.Id == image.Id, cancellationToken);

        if (existing is null)
        {
            return;
        }

        _dbContext.Entry(existing).CurrentValues.SetValues(image);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(SeoOgImage image, CancellationToken cancellationToken)
    {
        _dbContext.SeoOgImages.Remove(image);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteBySeoMetadataIdAsync(Guid seoMetadataId, CancellationToken cancellationToken)
    {
        var images = await _dbContext.SeoOgImages
            .Where(image => image.SeoMetadataId == seoMetadataId)
            .ToListAsync(cancellationToken);

        _dbContext.SeoOgImages.RemoveRange(images);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}


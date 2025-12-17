using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Attar.Domain.Entities.Seo;

namespace Attar.Application.Interfaces;

public interface ISeoOgImageRepository
{
    Task<SeoOgImage?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<SeoOgImage>> GetBySeoMetadataIdAsync(Guid seoMetadataId, CancellationToken cancellationToken);

    Task AddAsync(SeoOgImage image, CancellationToken cancellationToken);

    Task UpdateAsync(SeoOgImage image, CancellationToken cancellationToken);

    Task DeleteAsync(SeoOgImage image, CancellationToken cancellationToken);

    Task DeleteBySeoMetadataIdAsync(Guid seoMetadataId, CancellationToken cancellationToken);
}


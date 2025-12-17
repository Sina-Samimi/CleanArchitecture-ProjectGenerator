using System.Threading;
using System.Threading.Tasks;
using LogTableRenameTest.Domain.Entities.Seo;
using LogTableRenameTest.Domain.Enums;

namespace LogTableRenameTest.Application.Interfaces;

public interface ISeoMetadataRepository
{
    Task<SeoMetadata?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<SeoMetadata?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken);

    Task<SeoMetadata?> GetByPageTypeAndIdentifierAsync(SeoPageType pageType, string? pageIdentifier, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<SeoMetadata>> GetAllAsync(CancellationToken cancellationToken);

    Task<IReadOnlyCollection<SeoMetadata>> GetByPageTypeAsync(SeoPageType pageType, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(SeoPageType pageType, string? pageIdentifier, CancellationToken cancellationToken);

    Task AddAsync(SeoMetadata seoMetadata, CancellationToken cancellationToken);

    Task UpdateAsync(SeoMetadata seoMetadata, CancellationToken cancellationToken);

    Task DeleteAsync(SeoMetadata seoMetadata, CancellationToken cancellationToken);
}


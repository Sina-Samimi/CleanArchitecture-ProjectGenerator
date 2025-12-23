using System.Threading;
using System.Threading.Tasks;
using MobiRooz.Domain.Entities.Seo;
using MobiRooz.Domain.Enums;

namespace MobiRooz.Application.Interfaces;

public interface IPageFaqRepository
{
    Task<PageFaq?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<PageFaq?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<PageFaq>> GetByPageTypeAndIdentifierAsync(SeoPageType pageType, string? pageIdentifier, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<PageFaq>> GetAllAsync(CancellationToken cancellationToken);

    Task AddAsync(PageFaq pageFaq, CancellationToken cancellationToken);

    Task UpdateAsync(PageFaq pageFaq, CancellationToken cancellationToken);

    Task DeleteAsync(PageFaq pageFaq, CancellationToken cancellationToken);
}


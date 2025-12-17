using System.Threading;
using System.Threading.Tasks;
using LogsDtoCloneTest.Domain.Entities.Seo;
using LogsDtoCloneTest.Domain.Enums;

namespace LogsDtoCloneTest.Application.Interfaces;

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


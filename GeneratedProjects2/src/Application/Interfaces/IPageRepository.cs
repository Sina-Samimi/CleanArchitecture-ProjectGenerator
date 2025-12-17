using System.Threading;
using System.Threading.Tasks;
using LogsDtoCloneTest.Application.DTOs.Pages;
using LogsDtoCloneTest.Domain.Entities.Pages;

namespace LogsDtoCloneTest.Application.Interfaces;

public interface IPageRepository
{
    Task<Page?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<Page?> GetBySlugAsync(string slug, CancellationToken cancellationToken);

    Task<Page?> GetBySlugForUpdateAsync(string slug, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<Page>> GetAllAsync(CancellationToken cancellationToken);

    Task<IReadOnlyCollection<Page>> GetPublishedAsync(CancellationToken cancellationToken);

    Task<IReadOnlyCollection<Page>> GetFooterPagesAsync(CancellationToken cancellationToken);

    Task<IReadOnlyCollection<Page>> GetQuickAccessPagesAsync(CancellationToken cancellationToken);

    Task<bool> SlugExistsAsync(string slug, Guid? excludeId, CancellationToken cancellationToken);

    Task AddAsync(Page page, CancellationToken cancellationToken);

    Task UpdateAsync(Page page, CancellationToken cancellationToken);

    Task DeleteAsync(Page page, CancellationToken cancellationToken);

    Task<PageStatisticsDto> GetStatisticsAsync(CancellationToken cancellationToken);
}


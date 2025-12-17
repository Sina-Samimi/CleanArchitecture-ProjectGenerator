using System.Threading;
using System.Threading.Tasks;
using LogsDtoCloneTest.Application.DTOs.Seo;
using LogsDtoCloneTest.Domain.Enums;

namespace LogsDtoCloneTest.Application.Interfaces;

public interface ISeoMetadataService
{
    Task<SeoMetadataDto?> GetSeoMetadataAsync(SeoPageType pageType, string? pageIdentifier, CancellationToken cancellationToken);

    Task<PageFaqListDto> GetPageFaqsAsync(SeoPageType pageType, string? pageIdentifier, CancellationToken cancellationToken);
}


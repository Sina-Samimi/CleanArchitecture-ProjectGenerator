using System.Threading;
using System.Threading.Tasks;
using MobiRooz.Application.DTOs.Seo;
using MobiRooz.Domain.Enums;

namespace MobiRooz.Application.Interfaces;

public interface ISeoMetadataService
{
    Task<SeoMetadataDto?> GetSeoMetadataAsync(SeoPageType pageType, string? pageIdentifier, CancellationToken cancellationToken);

    Task<PageFaqListDto> GetPageFaqsAsync(SeoPageType pageType, string? pageIdentifier, CancellationToken cancellationToken);
}


using System.Threading;
using System.Threading.Tasks;
using Attar.Application.DTOs.Seo;
using Attar.Domain.Enums;

namespace Attar.Application.Interfaces;

public interface ISeoMetadataService
{
    Task<SeoMetadataDto?> GetSeoMetadataAsync(SeoPageType pageType, string? pageIdentifier, CancellationToken cancellationToken);

    Task<PageFaqListDto> GetPageFaqsAsync(SeoPageType pageType, string? pageIdentifier, CancellationToken cancellationToken);
}


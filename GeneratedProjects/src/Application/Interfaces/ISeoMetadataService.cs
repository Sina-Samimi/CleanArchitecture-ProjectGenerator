using System.Threading;
using System.Threading.Tasks;
using TestAttarClone.Application.DTOs.Seo;
using TestAttarClone.Domain.Enums;

namespace TestAttarClone.Application.Interfaces;

public interface ISeoMetadataService
{
    Task<SeoMetadataDto?> GetSeoMetadataAsync(SeoPageType pageType, string? pageIdentifier, CancellationToken cancellationToken);

    Task<PageFaqListDto> GetPageFaqsAsync(SeoPageType pageType, string? pageIdentifier, CancellationToken cancellationToken);
}


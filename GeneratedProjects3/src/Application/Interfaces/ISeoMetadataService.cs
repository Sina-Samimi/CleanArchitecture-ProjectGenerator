using System.Threading;
using System.Threading.Tasks;
using LogTableRenameTest.Application.DTOs.Seo;
using LogTableRenameTest.Domain.Enums;

namespace LogTableRenameTest.Application.Interfaces;

public interface ISeoMetadataService
{
    Task<SeoMetadataDto?> GetSeoMetadataAsync(SeoPageType pageType, string? pageIdentifier, CancellationToken cancellationToken);

    Task<PageFaqListDto> GetPageFaqsAsync(SeoPageType pageType, string? pageIdentifier, CancellationToken cancellationToken);
}


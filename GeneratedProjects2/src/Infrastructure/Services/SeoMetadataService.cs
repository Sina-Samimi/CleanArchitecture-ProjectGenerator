using System.Threading;
using System.Threading.Tasks;
using LogsDtoCloneTest.Application.DTOs.Seo;
using LogsDtoCloneTest.Application.Interfaces;
using LogsDtoCloneTest.Domain.Enums;
using MediatR;

namespace LogsDtoCloneTest.Infrastructure.Services;

public sealed class SeoMetadataService : ISeoMetadataService
{
    private readonly IMediator _mediator;

    public SeoMetadataService(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<SeoMetadataDto?> GetSeoMetadataAsync(SeoPageType pageType, string? pageIdentifier, CancellationToken cancellationToken)
    {
        var query = new Application.Queries.Seo.GetSeoMetadataQuery(pageType, pageIdentifier);
        var result = await _mediator.Send(query, cancellationToken);

        return result.IsSuccess ? result.Value : null;
    }

    public async Task<PageFaqListDto> GetPageFaqsAsync(SeoPageType pageType, string? pageIdentifier, CancellationToken cancellationToken)
    {
        var query = new Application.Queries.Seo.GetPageFaqsQuery(pageType, pageIdentifier);
        var result = await _mediator.Send(query, cancellationToken);

        return result.IsSuccess && result.Value is not null
            ? result.Value
            : new PageFaqListDto(System.Array.Empty<PageFaqDto>());
    }
}


using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LogTableRenameTest.Application.Abstractions.Messaging;
using LogTableRenameTest.Application.DTOs.Seo;
using LogTableRenameTest.Application.Interfaces;
using LogTableRenameTest.Domain.Enums;
using LogTableRenameTest.SharedKernel.BaseTypes;

namespace LogTableRenameTest.Application.Queries.Seo;

public sealed record GetSeoMetadataListQuery(SeoPageType? PageType = null) : IQuery<SeoMetadataListResultDto>
{
    public sealed class Handler : IQueryHandler<GetSeoMetadataListQuery, SeoMetadataListResultDto>
    {
        private readonly ISeoMetadataRepository _repository;

        public Handler(ISeoMetadataRepository repository)
        {
            _repository = repository;
        }

        public async Task<Result<SeoMetadataListResultDto>> Handle(GetSeoMetadataListQuery request, CancellationToken cancellationToken)
        {
            var allMetadata = request.PageType.HasValue
                ? await _repository.GetByPageTypeAsync(request.PageType.Value, cancellationToken)
                : await _repository.GetAllAsync(cancellationToken);

            var items = allMetadata
                .Select(seo => new SeoMetadataListItemDto(
                    seo.Id,
                    seo.PageType,
                    seo.PageIdentifier,
                    seo.MetaTitle,
                    seo.MetaDescription,
                    seo.MetaRobots,
                    seo.UpdateDate))
                .ToList();

            var result = new SeoMetadataListResultDto(items, items.Count);
            return Result<SeoMetadataListResultDto>.Success(result);
        }
    }
}


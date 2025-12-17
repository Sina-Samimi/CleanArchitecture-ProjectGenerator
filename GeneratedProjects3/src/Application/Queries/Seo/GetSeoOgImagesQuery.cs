using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LogTableRenameTest.Application.Abstractions.Messaging;
using LogTableRenameTest.Application.DTOs.Seo;
using LogTableRenameTest.Application.Interfaces;
using LogTableRenameTest.SharedKernel.BaseTypes;

namespace LogTableRenameTest.Application.Queries.Seo;

public sealed record GetSeoOgImagesQuery(Guid SeoMetadataId) : IQuery<IReadOnlyCollection<SeoOgImageDto>>
{
    public sealed class Handler : IQueryHandler<GetSeoOgImagesQuery, IReadOnlyCollection<SeoOgImageDto>>
    {
        private readonly ISeoOgImageRepository _repository;

        public Handler(ISeoOgImageRepository repository)
        {
            _repository = repository;
        }

        public async Task<Result<IReadOnlyCollection<SeoOgImageDto>>> Handle(GetSeoOgImagesQuery request, CancellationToken cancellationToken)
        {
            var images = await _repository.GetBySeoMetadataIdAsync(request.SeoMetadataId, cancellationToken);

            var dtos = images
                .Select(image => new SeoOgImageDto(
                    image.Id,
                    image.SeoMetadataId,
                    image.ImageUrl,
                    image.Width,
                    image.Height,
                    image.ImageType,
                    image.Alt,
                    image.DisplayOrder,
                    image.CreateDate,
                    image.UpdateDate))
                .ToList();

            return Result<IReadOnlyCollection<SeoOgImageDto>>.Success(dtos);
        }
    }
}


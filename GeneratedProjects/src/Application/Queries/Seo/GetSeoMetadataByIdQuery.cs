using System;
using System.Threading;
using System.Threading.Tasks;
using TestAttarClone.Application.Abstractions.Messaging;
using TestAttarClone.Application.DTOs.Seo;
using TestAttarClone.Application.Interfaces;
using TestAttarClone.SharedKernel.BaseTypes;

namespace TestAttarClone.Application.Queries.Seo;

public sealed record GetSeoMetadataByIdQuery(Guid Id) : IQuery<SeoMetadataDto?>
{
    public sealed class Handler : IQueryHandler<GetSeoMetadataByIdQuery, SeoMetadataDto?>
    {
        private readonly ISeoMetadataRepository _repository;

        public Handler(ISeoMetadataRepository repository)
        {
            _repository = repository;
        }

        public async Task<Result<SeoMetadataDto?>> Handle(GetSeoMetadataByIdQuery request, CancellationToken cancellationToken)
        {
            var seoMetadata = await _repository.GetByIdAsync(request.Id, cancellationToken);

            if (seoMetadata is null)
            {
                return Result<SeoMetadataDto?>.Success(null);
            }

            var dto = new SeoMetadataDto(
                seoMetadata.Id,
                seoMetadata.PageType,
                seoMetadata.PageIdentifier,
                seoMetadata.MetaTitle,
                seoMetadata.MetaDescription,
                seoMetadata.MetaKeywords,
                seoMetadata.MetaRobots,
                seoMetadata.CanonicalUrl,
                seoMetadata.UseTemplate,
                seoMetadata.TitleTemplate,
                seoMetadata.DescriptionTemplate,
                seoMetadata.OgTitleTemplate,
                seoMetadata.OgDescriptionTemplate,
                seoMetadata.RobotsTemplate,
                seoMetadata.OgTitle,
                seoMetadata.OgDescription,
                seoMetadata.OgImage,
                seoMetadata.OgType,
                seoMetadata.OgUrl,
                seoMetadata.TwitterCard,
                seoMetadata.TwitterTitle,
                seoMetadata.TwitterDescription,
                seoMetadata.TwitterImage,
                seoMetadata.SchemaJson,
                seoMetadata.BreadcrumbsJson,
                seoMetadata.SitemapPriority,
                seoMetadata.SitemapChangefreq,
                seoMetadata.H1Title,
                seoMetadata.FeaturedImageUrl,
                seoMetadata.FeaturedImageAlt,
                seoMetadata.Tags,
                seoMetadata.Description,
                seoMetadata.CreateDate,
                seoMetadata.UpdateDate);

            return Result<SeoMetadataDto?>.Success(dto);
        }
    }
}


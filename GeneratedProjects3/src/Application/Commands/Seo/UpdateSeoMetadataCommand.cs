using System;
using System.Threading;
using System.Threading.Tasks;
using LogTableRenameTest.Application.Abstractions.Messaging;
using LogTableRenameTest.Application.DTOs.Seo;
using LogTableRenameTest.Application.Interfaces;
using LogTableRenameTest.Domain.Enums;
using LogTableRenameTest.SharedKernel.BaseTypes;

namespace LogTableRenameTest.Application.Commands.Seo;

public sealed record UpdateSeoMetadataCommand(
    Guid Id,
    string? MetaTitle,
    string? MetaDescription,
    string? MetaKeywords,
    string? MetaRobots,
    string? CanonicalUrl,
    bool UseTemplate,
    string? TitleTemplate,
    string? DescriptionTemplate,
    string? OgTitleTemplate,
    string? OgDescriptionTemplate,
    string? RobotsTemplate,
    string? OgTitle,
    string? OgDescription,
    string? OgImage,
    string? OgType,
    string? OgUrl,
    string? TwitterCard,
    string? TwitterTitle,
    string? TwitterDescription,
    string? TwitterImage,
    string? SchemaJson,
    string? BreadcrumbsJson,
    decimal? SitemapPriority,
    string? SitemapChangefreq,
    string? H1Title,
    string? FeaturedImageUrl,
    string? FeaturedImageAlt,
    string? Tags,
    string? Description) : ICommand<SeoMetadataDto>
{
    public sealed class Handler : ICommandHandler<UpdateSeoMetadataCommand, SeoMetadataDto>
    {
        private readonly ISeoMetadataRepository _repository;

        public Handler(ISeoMetadataRepository repository)
        {
            _repository = repository;
        }

        public async Task<Result<SeoMetadataDto>> Handle(UpdateSeoMetadataCommand request, CancellationToken cancellationToken)
        {
            var seoMetadata = await _repository.GetByIdForUpdateAsync(request.Id, cancellationToken);

            if (seoMetadata is null)
            {
                return Result<SeoMetadataDto>.Failure("تنظیمات SEO مورد نظر یافت نشد.");
            }

            seoMetadata.UpdateSeo(
                seoMetadata.PageType,
                seoMetadata.PageIdentifier,
                request.MetaTitle,
                request.MetaDescription,
                request.MetaKeywords,
                request.MetaRobots,
                request.CanonicalUrl,
                request.UseTemplate,
                request.TitleTemplate,
                request.DescriptionTemplate,
                request.OgTitleTemplate,
                request.OgDescriptionTemplate,
                request.RobotsTemplate,
                request.OgTitle,
                request.OgDescription,
                request.OgImage,
                request.OgType,
                request.OgUrl,
                request.TwitterCard,
                request.TwitterTitle,
                request.TwitterDescription,
                request.TwitterImage,
                request.SchemaJson,
                request.BreadcrumbsJson,
                request.SitemapPriority,
                request.SitemapChangefreq,
                request.H1Title,
                request.FeaturedImageUrl,
                request.FeaturedImageAlt,
                request.Tags,
                request.Description);

            await _repository.UpdateAsync(seoMetadata, cancellationToken);

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

            return Result<SeoMetadataDto>.Success(dto);
        }
    }
}


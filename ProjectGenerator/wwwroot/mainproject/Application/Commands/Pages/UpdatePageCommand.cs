using System;
using System.Threading;
using System.Threading.Tasks;
using MobiRooz.Application.Abstractions.Messaging;
using MobiRooz.Application.DTOs.Pages;
using MobiRooz.Application.Interfaces;
using MobiRooz.SharedKernel.BaseTypes;

namespace MobiRooz.Application.Commands.Pages;

public sealed record UpdatePageCommand(
    Guid Id,
    string Title,
    string Slug,
    string Content,
    string? MetaTitle,
    string? MetaDescription,
    string? MetaKeywords,
    string? MetaRobots,
    bool IsPublished,
    string? FeaturedImagePath,
    bool ShowInFooter,
    bool ShowInQuickAccess) : ICommand<PageDto>
{
    public sealed class Handler : ICommandHandler<UpdatePageCommand, PageDto>
    {
        private readonly IPageRepository _repository;

        public Handler(IPageRepository repository)
        {
            _repository = repository;
        }

        public async Task<Result<PageDto>> Handle(UpdatePageCommand request, CancellationToken cancellationToken)
        {
            var page = await _repository.GetByIdAsync(request.Id, cancellationToken);
            if (page is null)
            {
                return Result<PageDto>.Failure("صفحه مورد نظر یافت نشد.");
            }

            // Check if slug already exists (excluding current page)
            var slugExists = await _repository.SlugExistsAsync(request.Slug, request.Id, cancellationToken);
            if (slugExists)
            {
                return Result<PageDto>.Failure($"صفحه‌ای با آدرس '{request.Slug}' از قبل وجود دارد.");
            }

            page.UpdateContent(request.Title, request.Slug, request.Content);
            page.UpdateSeo(request.MetaTitle, request.MetaDescription, request.MetaKeywords, request.MetaRobots);
            page.SetFeaturedImage(request.FeaturedImagePath);
            page.SetDisplayOptions(request.ShowInFooter, request.ShowInQuickAccess);

            if (request.IsPublished && !page.IsPublished)
            {
                page.Publish();
            }
            else if (!request.IsPublished && page.IsPublished)
            {
                page.Unpublish();
            }

            await _repository.UpdateAsync(page, cancellationToken);

            var dto = new PageDto(
                page.Id,
                page.Title,
                page.Slug,
                page.Content,
                page.MetaTitle,
                page.MetaDescription,
                page.MetaKeywords,
                page.MetaRobots,
                page.IsPublished,
                page.PublishedAt,
                page.ViewCount,
                page.FeaturedImagePath,
                page.ShowInFooter,
                page.ShowInQuickAccess,
                page.CreateDate,
                page.UpdateDate);

            return Result<PageDto>.Success(dto);
        }
    }
}


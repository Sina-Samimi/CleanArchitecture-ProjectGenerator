using System;
using System.Threading;
using System.Threading.Tasks;
using Attar.Application.Abstractions.Messaging;
using Attar.Application.DTOs.Pages;
using Attar.Application.Interfaces;
using Attar.Domain.Entities.Pages;
using Attar.SharedKernel.BaseTypes;

namespace Attar.Application.Commands.Pages;

public sealed record CreatePageCommand(
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
    public sealed class Handler : ICommandHandler<CreatePageCommand, PageDto>
    {
        private readonly IPageRepository _repository;

        public Handler(IPageRepository repository)
        {
            _repository = repository;
        }

        public async Task<Result<PageDto>> Handle(CreatePageCommand request, CancellationToken cancellationToken)
        {
            // Check if slug already exists
            var slugExists = await _repository.SlugExistsAsync(request.Slug, null, cancellationToken);
            if (slugExists)
            {
                return Result<PageDto>.Failure($"صفحه‌ای با آدرس '{request.Slug}' از قبل وجود دارد.");
            }

            var page = new Page(
                request.Title,
                request.Slug,
                request.Content,
                request.MetaTitle,
                request.MetaDescription,
                request.MetaKeywords,
                request.MetaRobots,
                request.IsPublished,
                publishedAt: null,
                request.FeaturedImagePath,
                request.ShowInFooter,
                request.ShowInQuickAccess);

            await _repository.AddAsync(page, cancellationToken);

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


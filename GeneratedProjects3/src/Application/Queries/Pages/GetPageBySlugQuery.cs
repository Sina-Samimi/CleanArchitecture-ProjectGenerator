using System;
using System.Threading;
using System.Threading.Tasks;
using LogTableRenameTest.Application.Abstractions.Messaging;
using LogTableRenameTest.Application.DTOs.Pages;
using LogTableRenameTest.Application.Interfaces;
using LogTableRenameTest.SharedKernel.BaseTypes;

namespace LogTableRenameTest.Application.Queries.Pages;

public sealed record GetPageBySlugQuery(string Slug) : IQuery<PageDto>
{
    public sealed class Handler : IQueryHandler<GetPageBySlugQuery, PageDto>
    {
        private readonly IPageRepository _repository;

        public Handler(IPageRepository repository)
        {
            _repository = repository;
        }

        public async Task<Result<PageDto>> Handle(GetPageBySlugQuery request, CancellationToken cancellationToken)
        {
            var page = await _repository.GetBySlugForUpdateAsync(request.Slug, cancellationToken);
            if (page is null)
            {
                return Result<PageDto>.Failure("صفحه مورد نظر یافت نشد.");
            }

            // Increment view count
            page.IncrementViewCount();
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


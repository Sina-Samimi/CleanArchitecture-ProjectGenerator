using System;
using System.Threading;
using System.Threading.Tasks;
using Attar.Application.Abstractions.Messaging;
using Attar.Application.DTOs.Pages;
using Attar.Application.Interfaces;
using Attar.SharedKernel.BaseTypes;

namespace Attar.Application.Queries.Pages;

public sealed record GetPageByIdQuery(Guid Id) : IQuery<PageDto>
{
    public sealed class Handler : IQueryHandler<GetPageByIdQuery, PageDto>
    {
        private readonly IPageRepository _repository;

        public Handler(IPageRepository repository)
        {
            _repository = repository;
        }

        public async Task<Result<PageDto>> Handle(GetPageByIdQuery request, CancellationToken cancellationToken)
        {
            var page = await _repository.GetByIdAsync(request.Id, cancellationToken);
            if (page is null)
            {
                return Result<PageDto>.Failure("صفحه مورد نظر یافت نشد.");
            }

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


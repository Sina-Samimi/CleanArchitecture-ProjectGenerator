using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TestAttarClone.Application.Abstractions.Messaging;
using TestAttarClone.Application.DTOs.Pages;
using TestAttarClone.Application.Interfaces;
using TestAttarClone.SharedKernel.BaseTypes;

namespace TestAttarClone.Application.Queries.Pages;

public sealed record GetPagesQuery(bool? PublishedOnly = null) : IQuery<PageListResultDto>
{
    public sealed class Handler : IQueryHandler<GetPagesQuery, PageListResultDto>
    {
        private readonly IPageRepository _repository;

        public Handler(IPageRepository repository)
        {
            _repository = repository;
        }

        public async Task<Result<PageListResultDto>> Handle(GetPagesQuery request, CancellationToken cancellationToken)
        {
            IReadOnlyCollection<Domain.Entities.Pages.Page> pages;

            if (request.PublishedOnly == true)
            {
                pages = await _repository.GetPublishedAsync(cancellationToken);
            }
            else
            {
                pages = await _repository.GetAllAsync(cancellationToken);
            }

            var pageDtos = pages.Select(page => new PageListItemDto(
                page.Id,
                page.Title,
                page.Slug,
                page.IsPublished,
                page.PublishedAt,
                page.ViewCount,
                page.CreateDate)).ToArray();

            var result = new PageListResultDto(pageDtos, pageDtos.Length);

            return Result<PageListResultDto>.Success(result);
        }
    }
}


using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TestAttarClone.Application.Abstractions.Messaging;
using TestAttarClone.Application.DTOs.Blogs;
using TestAttarClone.Application.Interfaces;
using TestAttarClone.Domain.Enums;
using TestAttarClone.SharedKernel.BaseTypes;

namespace TestAttarClone.Application.Queries.Blogs;

public sealed record GetBlogListQuery(
    string? Search,
    Guid? CategoryId,
    Guid? AuthorId,
    BlogStatus? Status,
    DateOnly? FromDate,
    DateOnly? ToDate,
    int Page,
    int PageSize) : IQuery<BlogListResultDto>
{
    public sealed class Handler : IQueryHandler<GetBlogListQuery, BlogListResultDto>
    {
        private readonly IBlogRepository _blogRepository;
        private readonly IBlogCategoryRepository _blogCategoryRepository;

        public Handler(IBlogRepository blogRepository, IBlogCategoryRepository blogCategoryRepository)
        {
            _blogRepository = blogRepository;
            _blogCategoryRepository = blogCategoryRepository;
        }

        public async Task<Result<BlogListResultDto>> Handle(GetBlogListQuery request, CancellationToken cancellationToken)
        {
            var page = request.Page <= 0 ? 1 : request.Page;
            var pageSize = request.PageSize <= 0 ? 10 : Math.Clamp(request.PageSize, 5, 50);

            IReadOnlyCollection<Guid>? categoryScope = null;
            if (request.CategoryId.HasValue)
            {
                categoryScope = await _blogCategoryRepository
                    .GetDescendantIdsAsync(request.CategoryId.Value, cancellationToken);

                if (categoryScope.Count == 0)
                {
                    return Result<BlogListResultDto>.Success(new BlogListResultDto(
                        Array.Empty<BlogListItemDto>(),
                        0,
                        0,
                        page,
                        pageSize,
                        1,
                        new BlogStatisticsDto(0, 0, 0, 0, 0, 0, 0, 0)));
                }
            }

            var filter = new BlogListFilterDto(
                request.Search?.Trim(),
                request.CategoryId,
                request.AuthorId,
                request.Status,
                request.FromDate,
                request.ToDate,
                page,
                pageSize);

            var result = await _blogRepository.GetListAsync(filter, categoryScope, cancellationToken);

            return Result<BlogListResultDto>.Success(result);
        }
    }
}

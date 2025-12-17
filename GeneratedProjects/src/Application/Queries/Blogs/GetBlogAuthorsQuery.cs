using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TestAttarClone.Application.Abstractions.Messaging;
using TestAttarClone.Application.DTOs.Blogs;
using TestAttarClone.Application.Interfaces;
using TestAttarClone.SharedKernel.BaseTypes;

namespace TestAttarClone.Application.Queries.Blogs;

public sealed record GetBlogAuthorsQuery() : IQuery<BlogAuthorListResultDto>
{
    public sealed class Handler : IQueryHandler<GetBlogAuthorsQuery, BlogAuthorListResultDto>
    {
        private readonly IBlogAuthorRepository _authorRepository;

        public Handler(IBlogAuthorRepository authorRepository)
        {
            _authorRepository = authorRepository;
        }

        public async Task<Result<BlogAuthorListResultDto>> Handle(GetBlogAuthorsQuery request, CancellationToken cancellationToken)
        {
            var authors = await _authorRepository.GetAllAsync(cancellationToken);
            if (authors.Count == 0)
            {
                return Result<BlogAuthorListResultDto>.Success(new BlogAuthorListResultDto(Array.Empty<BlogAuthorListItemDto>(), 0, 0));
            }

            var items = authors
                .OrderBy(author => author.DisplayName, StringComparer.CurrentCulture)
                .Select(author => new BlogAuthorListItemDto(
                    author.Id,
                    author.DisplayName,
                    author.Bio,
                    author.AvatarUrl,
                    author.IsActive,
                    author.UserId,
                    author.User?.FullName,
                    author.User?.Email,
                    author.User?.PhoneNumber,
                    author.CreateDate,
                    author.UpdateDate))
                .ToArray();

            var activeCount = items.Count(author => author.IsActive);
            var inactiveCount = items.Length - activeCount;

            return Result<BlogAuthorListResultDto>.Success(new BlogAuthorListResultDto(items, activeCount, inactiveCount));
        }
    }
}

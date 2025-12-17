using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TestAttarClone.Application.Abstractions.Messaging;
using TestAttarClone.Application.DTOs.Blogs;
using TestAttarClone.Application.Interfaces;
using TestAttarClone.SharedKernel.BaseTypes;

namespace TestAttarClone.Application.Queries.Blogs;

public sealed record GetBlogCommentsQuery(Guid BlogId) : IQuery<BlogCommentListResultDto>
{
    public sealed class Handler : IQueryHandler<GetBlogCommentsQuery, BlogCommentListResultDto>
    {
        private readonly IBlogRepository _blogRepository;
        private readonly IBlogCommentRepository _blogCommentRepository;

        public Handler(IBlogRepository blogRepository, IBlogCommentRepository blogCommentRepository)
        {
            _blogRepository = blogRepository;
            _blogCommentRepository = blogCommentRepository;
        }

        public async Task<Result<BlogCommentListResultDto>> Handle(GetBlogCommentsQuery request, CancellationToken cancellationToken)
        {
            if (request.BlogId == Guid.Empty)
            {
                return Result<BlogCommentListResultDto>.Failure("شناسه بلاگ معتبر نیست.");
            }

            var blog = await _blogRepository.GetByIdAsync(request.BlogId, cancellationToken);
            if (blog is null)
            {
                return Result<BlogCommentListResultDto>.Failure("بلاگ مورد نظر یافت نشد.");
            }

            var comments = await _blogCommentRepository.GetByBlogIdAsync(request.BlogId, cancellationToken);

            var commentDtos = comments
                .OrderBy(comment => comment.CreateDate)
                .Select(comment => new BlogCommentDto(
                    comment.Id,
                    comment.BlogId,
                    comment.ParentId,
                    comment.AuthorName,
                    comment.AuthorEmail,
                    comment.Content,
                    comment.IsApproved,
                    comment.CreateDate,
                    comment.UpdateDate,
                    comment.ApprovedById,
                    comment.ApprovedBy?.FullName,
                    comment.ApprovedAt))
                .ToArray();

            var blogSummary = new BlogSummaryDto(blog.Id, blog.Title, blog.SeoSlug);

            return Result<BlogCommentListResultDto>.Success(new BlogCommentListResultDto(blogSummary, commentDtos));
        }
    }
}

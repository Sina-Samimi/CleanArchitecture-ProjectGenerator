using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MobiRooz.Application.Abstractions.Messaging;
using MobiRooz.Application.DTOs.Blogs;
using MobiRooz.Application.Interfaces;
using MobiRooz.SharedKernel.BaseTypes;

namespace MobiRooz.Application.Queries.Blogs;

public sealed record GetBlogDetailQuery(Guid Id) : IQuery<BlogDetailDto>
{
    public sealed class Handler : IQueryHandler<GetBlogDetailQuery, BlogDetailDto>
    {
        private readonly IBlogRepository _blogRepository;

        public Handler(IBlogRepository blogRepository)
        {
            _blogRepository = blogRepository;
        }

        public async Task<Result<BlogDetailDto>> Handle(GetBlogDetailQuery request, CancellationToken cancellationToken)
        {
            if (request.Id == Guid.Empty)
            {
                return Result<BlogDetailDto>.Failure("شناسه بلاگ معتبر نیست.");
            }

            var blog = await _blogRepository.GetWithDetailsAsync(request.Id, cancellationToken);
            if (blog is null)
            {
                return Result<BlogDetailDto>.Failure("بلاگ مورد نظر یافت نشد.");
            }

            var dto = new BlogDetailDto(
                blog.Id,
                blog.Title,
                blog.Summary,
                blog.Content,
                blog.CategoryId,
                blog.AuthorId,
                blog.Status,
                blog.ReadingTimeMinutes,
                blog.PublishedAt,
                blog.SeoTitle,
                blog.SeoDescription,
                blog.SeoKeywords,
                blog.SeoSlug,
                blog.LikeCount,
                blog.DislikeCount,
                blog.Views.Count,
                blog.Comments.Count(comment => comment.IsApproved),
                blog.FeaturedImagePath,
                blog.Robots,
                blog.TagList ?? string.Empty);

            return Result<BlogDetailDto>.Success(dto);
        }
    }
}

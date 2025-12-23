using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MobiRooz.Application.Abstractions.Messaging;
using MobiRooz.Application.Interfaces;
using MobiRooz.Domain.Entities.Blogs;
using MobiRooz.Domain.Enums;
using MobiRooz.SharedKernel.BaseTypes;

namespace MobiRooz.Application.Commands.Blogs;

public sealed record CreateBlogCommand(
    string Title,
    string Summary,
    string Content,
    Guid CategoryId,
    Guid AuthorId,
    BlogStatus Status,
    int ReadingTimeMinutes,
    DateTimeOffset? PublishedAt,
    string SeoTitle,
    string SeoDescription,
    string SeoKeywords,
    string SeoSlug,
    string? FeaturedImagePath,
    string Robots,
    IReadOnlyCollection<string> Tags) : ICommand<Guid>
{
    public sealed class Handler : ICommandHandler<CreateBlogCommand, Guid>
    {
        private readonly IBlogRepository _blogRepository;
        private readonly IBlogCategoryRepository _categoryRepository;
        private readonly IBlogAuthorRepository _authorRepository;
        private readonly IAuditContext _auditContext;

        public Handler(
            IBlogRepository blogRepository,
            IBlogCategoryRepository categoryRepository,
            IBlogAuthorRepository authorRepository,
            IAuditContext auditContext)
        {
            _blogRepository = blogRepository;
            _categoryRepository = categoryRepository;
            _authorRepository = authorRepository;
            _auditContext = auditContext;
        }

        public async Task<Result<Guid>> Handle(CreateBlogCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Title))
            {
                return Result<Guid>.Failure("عنوان بلاگ الزامی است.");
            }

            if (string.IsNullOrWhiteSpace(request.Content))
            {
                return Result<Guid>.Failure("محتوای بلاگ نمی‌تواند خالی باشد.");
            }

            if (request.ReadingTimeMinutes <= 0)
            {
                return Result<Guid>.Failure("مدت زمان مطالعه باید بزرگتر از صفر باشد.");
            }

            var category = await _categoryRepository.GetByIdAsync(request.CategoryId, cancellationToken);
            if (category is null)
            {
                return Result<Guid>.Failure("دسته‌بندی انتخاب شده وجود ندارد.");
            }

            var author = await _authorRepository.GetByIdAsync(request.AuthorId, cancellationToken);
            if (author is null || !author.IsActive)
            {
                return Result<Guid>.Failure("نویسنده انتخاب شده معتبر نیست.");
            }

            var audit = _auditContext.Capture();

            var blog = new Blog(
                request.Title,
                request.Summary,
                request.Content,
                category,
                author,
                request.Status,
                request.ReadingTimeMinutes,
                request.SeoTitle,
                request.SeoDescription,
                request.SeoKeywords,
                request.SeoSlug,
                request.Robots,
                request.FeaturedImagePath,
                request.Tags,
                request.PublishedAt)
            {
                CreatorId = audit.UserId,
                CreateDate = audit.Timestamp,
                UpdateDate = audit.Timestamp,
                Ip = audit.IpAddress
            };

            await _blogRepository.AddAsync(blog, cancellationToken);

            return Result<Guid>.Success(blog.Id);
        }
    }
}

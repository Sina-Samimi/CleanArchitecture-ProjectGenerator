using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LogsDtoCloneTest.Application.Abstractions.Messaging;
using LogsDtoCloneTest.Application.Interfaces;
using LogsDtoCloneTest.Domain.Enums;
using LogsDtoCloneTest.SharedKernel.BaseTypes;

namespace LogsDtoCloneTest.Application.Commands.Blogs;

public sealed record UpdateBlogCommand(
    Guid Id,
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
    IReadOnlyCollection<string> Tags) : ICommand
{
    public sealed class Handler : ICommandHandler<UpdateBlogCommand>
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

        public async Task<Result> Handle(UpdateBlogCommand request, CancellationToken cancellationToken)
        {
            if (request.Id == Guid.Empty)
            {
                return Result.Failure("شناسه بلاگ معتبر نیست.");
            }

            var blog = await _blogRepository.GetWithDetailsAsync(request.Id, cancellationToken);
            if (blog is null)
            {
                return Result.Failure("بلاگ مورد نظر یافت نشد.");
            }

            var category = await _categoryRepository.GetByIdForUpdateAsync(request.CategoryId, cancellationToken);
            if (category is null)
            {
                return Result.Failure("دسته‌بندی انتخاب شده وجود ندارد.");
            }

            var author = await _authorRepository.GetByIdForUpdateAsync(request.AuthorId, cancellationToken);
            if (author is null || !author.IsActive)
            {
                return Result.Failure("نویسنده انتخاب شده معتبر نیست.");
            }

            if (string.IsNullOrWhiteSpace(request.Title))
            {
                return Result.Failure("عنوان بلاگ الزامی است.");
            }

            if (string.IsNullOrWhiteSpace(request.Content))
            {
                return Result.Failure("محتوای بلاگ نمی‌تواند خالی باشد.");
            }

            if (request.ReadingTimeMinutes <= 0)
            {
                return Result.Failure("مدت زمان مطالعه باید بزرگتر از صفر باشد.");
            }

            blog.UpdateContent(request.Title, request.Summary, request.Content, request.ReadingTimeMinutes);
            blog.SetCategory(category);
            blog.SetAuthor(author);

            var nextPublishedAt = request.Status == BlogStatus.Published
                ? request.PublishedAt ?? blog.PublishedAt
                : request.PublishedAt;

            blog.ChangeStatus(request.Status, nextPublishedAt);
            blog.UpdateSeoMetadata(request.SeoTitle, request.SeoDescription, request.SeoKeywords, request.SeoSlug, request.Robots);
            blog.SetFeaturedImage(request.FeaturedImagePath);
            blog.SetTags(request.Tags);

            var audit = _auditContext.Capture();
            blog.UpdaterId = audit.UserId;
            blog.UpdateDate = audit.Timestamp;
            blog.Ip = audit.IpAddress;

            await _blogRepository.UpdateAsync(blog, cancellationToken);

            return Result.Success();
        }
    }
}

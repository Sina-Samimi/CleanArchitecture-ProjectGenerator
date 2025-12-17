using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LogTableRenameTest.Application.Abstractions.Messaging;
using LogTableRenameTest.Application.Interfaces;
using LogTableRenameTest.SharedKernel.BaseTypes;

namespace LogTableRenameTest.Application.Commands.Blogs;

public sealed record DeleteBlogCategoryCommand(Guid Id) : ICommand
{
    public sealed class Handler : ICommandHandler<DeleteBlogCategoryCommand>
    {
        private readonly IBlogCategoryRepository _categoryRepository;
        private readonly IBlogRepository _blogRepository;
        private readonly IAuditContext _auditContext;

        public Handler(
            IBlogCategoryRepository categoryRepository,
            IBlogRepository blogRepository,
            IAuditContext auditContext)
        {
            _categoryRepository = categoryRepository;
            _blogRepository = blogRepository;
            _auditContext = auditContext;
        }

        public async Task<Result> Handle(DeleteBlogCategoryCommand request, CancellationToken cancellationToken)
        {
            if (request.Id == Guid.Empty)
            {
                return Result.Failure("شناسه دسته‌بندی معتبر نیست.");
            }

            var category = await _categoryRepository.GetByIdForUpdateAsync(request.Id, cancellationToken);
            if (category is null)
            {
                return Result.Failure("دسته‌بندی مورد نظر یافت نشد.");
            }

            var scope = await _categoryRepository.GetDescendantIdsAsync(request.Id, cancellationToken);
            if (scope.Any(childId => childId != request.Id))
            {
                return Result.Failure("ابتدا زیرمجموعه‌های این دسته‌بندی را حذف یا جابجا کنید.");
            }

            if (await _blogRepository.ExistsInCategoriesAsync(scope, cancellationToken))
            {
                return Result.Failure("ابتدا بلاگ‌های مرتبط با این دسته‌بندی را منتقل یا حذف کنید.");
            }

            var audit = _auditContext.Capture();

            category.IsDeleted = true;
            category.RemoveDate = audit.Timestamp;
            category.UpdaterId = audit.UserId;
            category.UpdateDate = audit.Timestamp;
            category.Ip = audit.IpAddress;

            await _categoryRepository.RemoveAsync(category, cancellationToken);

            return Result.Success();
        }
    }
}

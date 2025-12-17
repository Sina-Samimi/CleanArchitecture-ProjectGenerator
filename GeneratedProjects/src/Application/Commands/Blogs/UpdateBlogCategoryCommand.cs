using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TestAttarClone.Application.Abstractions.Messaging;
using TestAttarClone.Application.Interfaces;
using TestAttarClone.Domain.Entities.Blogs;
using TestAttarClone.SharedKernel.BaseTypes;

namespace TestAttarClone.Application.Commands.Blogs;

public sealed record UpdateBlogCategoryCommand(
    Guid Id,
    string Name,
    string? Slug,
    string? Description,
    Guid? ParentId) : ICommand
{
    public sealed class Handler : ICommandHandler<UpdateBlogCategoryCommand>
    {
        private readonly IBlogCategoryRepository _categoryRepository;
        private readonly IAuditContext _auditContext;

        public Handler(IBlogCategoryRepository categoryRepository, IAuditContext auditContext)
        {
            _categoryRepository = categoryRepository;
            _auditContext = auditContext;
        }

        public async Task<Result> Handle(UpdateBlogCategoryCommand request, CancellationToken cancellationToken)
        {
            if (request.Id == Guid.Empty)
            {
                return Result.Failure("شناسه دسته‌بندی معتبر نیست.");
            }

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return Result.Failure("نام دسته‌بندی الزامی است.");
            }

            var category = await _categoryRepository.GetByIdForUpdateAsync(request.Id, cancellationToken);
            if (category is null)
            {
                return Result.Failure("دسته‌بندی مورد نظر یافت نشد.");
            }

            BlogCategory? parent = null;
            if (request.ParentId is Guid parentId)
            {
                if (parentId == request.Id)
                {
                    return Result.Failure("یک دسته‌بندی نمی‌تواند والد خودش باشد.");
                }

                var descendants = await _categoryRepository.GetDescendantIdsAsync(request.Id, cancellationToken);
                if (descendants.Any(childId => childId == parentId))
                {
                    return Result.Failure("انتخاب فرزند به عنوان والد مجاز نیست.");
                }

                parent = await _categoryRepository.GetByIdAsync(parentId, cancellationToken);
                if (parent is null)
                {
                    return Result.Failure("دسته‌بندی والد انتخاب شده وجود ندارد.");
                }
            }

            category.Update(request.Name, request.Slug ?? string.Empty, request.Description);
            category.SetParent(parent);

            var slugExists = await _categoryRepository.ExistsBySlugAsync(category.Slug, category.Id, cancellationToken);
            if (slugExists)
            {
                return Result.Failure("مسیر انتخاب شده تکراری است.");
            }

            var audit = _auditContext.Capture();

            category.UpdaterId = audit.UserId;
            category.UpdateDate = audit.Timestamp;
            category.Ip = audit.IpAddress;

            await _categoryRepository.UpdateAsync(category, cancellationToken);

            return Result.Success();
        }
    }
}

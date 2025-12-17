using System;
using System.Threading;
using System.Threading.Tasks;
using TestAttarClone.Application.Abstractions.Messaging;
using TestAttarClone.Application.Interfaces;
using TestAttarClone.Domain.Entities.Catalog;
using TestAttarClone.Domain.Enums;
using TestAttarClone.SharedKernel.BaseTypes;

namespace TestAttarClone.Application.Commands.Catalog;

public sealed record UpdateSiteCategoryCommand(
    Guid Id,
    string Name,
    string? Slug,
    string? Description,
    CategoryScope Scope,
    Guid? ParentId,
    string? ImageUrl = null) : ICommand
{
    public sealed class Handler : ICommandHandler<UpdateSiteCategoryCommand>
    {
        private readonly ISiteCategoryRepository _categoryRepository;
        private readonly IAuditContext _auditContext;

        public Handler(ISiteCategoryRepository categoryRepository, IAuditContext auditContext)
        {
            _categoryRepository = categoryRepository;
            _auditContext = auditContext;
        }

        public async Task<Result> Handle(UpdateSiteCategoryCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return Result.Failure("نام دسته‌بندی الزامی است.");
            }

            var category = await _categoryRepository.GetByIdAsync(request.Id, cancellationToken);
            if (category is null)
            {
                return Result.Failure("دسته‌بندی مورد نظر یافت نشد.");
            }

            if (!Enum.IsDefined(request.Scope))
            {
                return Result.Failure("دامنه دسته‌بندی معتبر نیست.");
            }

            if (request.ParentId == request.Id)
            {
                return Result.Failure("یک دسته‌بندی نمی‌تواند والد خودش باشد.");
            }

            var slugExists = await _categoryRepository.ExistsBySlugAsync(
                request.Slug ?? category.Slug,
                request.Scope,
                request.Id,
                cancellationToken);

            if (slugExists)
            {
                return Result.Failure("مسیر انتخاب شده تکراری است.");
            }

            SiteCategory? parent = null;
            if (request.ParentId is Guid parentId)
            {
                parent = await _categoryRepository.GetByIdAsync(parentId, cancellationToken);
                if (parent is null)
                {
                    return Result.Failure("دسته‌بندی والد انتخاب شده وجود ندارد.");
                }

                if (parent.Scope != request.Scope)
                {
                    return Result.Failure("دامنه دسته‌بندی فرزند باید با والد یکسان باشد.");
                }
            }

            category.Update(request.Name, request.Slug ?? category.Slug, request.Description, request.Scope, request.ImageUrl);
            category.SetParent(parent);

            var audit = _auditContext.Capture();
            category.UpdaterId = audit.UserId;
            category.UpdateDate = audit.Timestamp;
            category.Ip = audit.IpAddress;

            await _categoryRepository.UpdateAsync(category, cancellationToken);

            return Result.Success();
        }
    }
}

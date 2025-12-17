using System;
using System.Threading;
using System.Threading.Tasks;
using LogTableRenameTest.Application.Abstractions.Messaging;
using LogTableRenameTest.Application.Interfaces;
using LogTableRenameTest.Domain.Entities.Catalog;
using LogTableRenameTest.Domain.Enums;
using LogTableRenameTest.SharedKernel.BaseTypes;

namespace LogTableRenameTest.Application.Commands.Catalog;

public sealed record CreateSiteCategoryCommand(
    string Name,
    string? Slug,
    string? Description,
    CategoryScope Scope,
    Guid? ParentId,
    string? ImageUrl = null) : ICommand<Guid>
{
    public sealed class Handler : ICommandHandler<CreateSiteCategoryCommand, Guid>
    {
        private readonly ISiteCategoryRepository _categoryRepository;
        private readonly IAuditContext _auditContext;

        public Handler(ISiteCategoryRepository categoryRepository, IAuditContext auditContext)
        {
            _categoryRepository = categoryRepository;
            _auditContext = auditContext;
        }

        public async Task<Result<Guid>> Handle(CreateSiteCategoryCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return Result<Guid>.Failure("نام دسته‌بندی الزامی است.");
            }

            if (!Enum.IsDefined(request.Scope))
            {
                return Result<Guid>.Failure("دامنه دسته‌بندی معتبر نیست.");
            }

            SiteCategory? parent = null;
            if (request.ParentId is Guid parentId)
            {
                parent = await _categoryRepository.GetByIdAsync(parentId, cancellationToken);
                if (parent is null)
                {
                    return Result<Guid>.Failure("دسته‌بندی والد انتخاب شده وجود ندارد.");
                }

                if (parent.Scope != request.Scope)
                {
                    return Result<Guid>.Failure("دامنه دسته‌بندی فرزند باید با والد یکسان باشد.");
                }
            }

            var category = new SiteCategory(
                request.Name,
                request.Slug ?? string.Empty,
                request.Scope,
                request.Description,
                parent,
                request.ImageUrl);

            var slugExists = await _categoryRepository.ExistsBySlugAsync(
                category.Slug,
                category.Scope,
                null,
                cancellationToken);

            if (slugExists)
            {
                return Result<Guid>.Failure("مسیر انتخاب شده تکراری است.");
            }

            var audit = _auditContext.Capture();

            category.CreatorId = audit.UserId;
            category.CreateDate = audit.Timestamp;
            category.UpdateDate = audit.Timestamp;
            category.Ip = audit.IpAddress;

            await _categoryRepository.AddAsync(category, cancellationToken);

            return Result<Guid>.Success(category.Id);
        }
    }
}

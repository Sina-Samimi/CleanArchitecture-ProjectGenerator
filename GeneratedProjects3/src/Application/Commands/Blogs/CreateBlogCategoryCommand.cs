using System;
using System.Threading;
using System.Threading.Tasks;
using LogTableRenameTest.Application.Abstractions.Messaging;
using LogTableRenameTest.Application.Interfaces;
using LogTableRenameTest.Domain.Entities.Blogs;
using LogTableRenameTest.SharedKernel.BaseTypes;

namespace LogTableRenameTest.Application.Commands.Blogs;

public sealed record CreateBlogCategoryCommand(
    string Name,
    string? Slug,
    string? Description,
    Guid? ParentId) : ICommand<Guid>
{
    public sealed class Handler : ICommandHandler<CreateBlogCategoryCommand, Guid>
    {
        private readonly IBlogCategoryRepository _categoryRepository;
        private readonly IAuditContext _auditContext;

        public Handler(IBlogCategoryRepository categoryRepository, IAuditContext auditContext)
        {
            _categoryRepository = categoryRepository;
            _auditContext = auditContext;
        }

        public async Task<Result<Guid>> Handle(CreateBlogCategoryCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return Result<Guid>.Failure("نام دسته‌بندی الزامی است.");
            }

            BlogCategory? parent = null;
            if (request.ParentId is Guid parentId)
            {
                parent = await _categoryRepository.GetByIdAsync(parentId, cancellationToken);
                if (parent is null)
                {
                    return Result<Guid>.Failure("دسته‌بندی والد انتخاب شده وجود ندارد.");
                }
            }

            var category = new BlogCategory(request.Name, request.Slug ?? string.Empty, request.Description, parent);

            var slugExists = await _categoryRepository.ExistsBySlugAsync(category.Slug, null, cancellationToken);
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

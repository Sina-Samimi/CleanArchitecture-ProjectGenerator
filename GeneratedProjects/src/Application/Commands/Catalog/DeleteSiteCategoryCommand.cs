using System;
using System.Threading;
using System.Threading.Tasks;
using TestAttarClone.Application.Abstractions.Messaging;
using TestAttarClone.Application.Interfaces;
using TestAttarClone.SharedKernel.BaseTypes;

namespace TestAttarClone.Application.Commands.Catalog;

public sealed record DeleteSiteCategoryCommand(Guid Id) : ICommand
{
    public sealed class Handler : ICommandHandler<DeleteSiteCategoryCommand>
    {
        private readonly ISiteCategoryRepository _categoryRepository;
        private readonly IProductRepository _productRepository;
        private readonly IAuditContext _auditContext;

        public Handler(
            ISiteCategoryRepository categoryRepository,
            IProductRepository productRepository,
            IAuditContext auditContext)
        {
            _categoryRepository = categoryRepository;
            _productRepository = productRepository;
            _auditContext = auditContext;
        }

        public async Task<Result> Handle(DeleteSiteCategoryCommand request, CancellationToken cancellationToken)
        {
            var category = await _categoryRepository.GetByIdAsync(request.Id, cancellationToken);
            if (category is null)
            {
                return Result.Failure("دسته‌بندی مورد نظر یافت نشد.");
            }

            var hasChildren = await _categoryRepository.HasChildrenAsync(category.Id, cancellationToken);
            if (hasChildren)
            {
                return Result.Failure("امکان حذف دسته‌بندی دارای زیرمجموعه وجود ندارد.");
            }

            var isUsed = await _productRepository.ExistsInCategoriesAsync(new[] { category.Id }, cancellationToken);
            if (isUsed)
            {
                return Result.Failure("این دسته‌بندی در محصولات استفاده شده است و قابل حذف نیست.");
            }

            var audit = _auditContext.Capture();

            category.IsDeleted = true;
            category.RemoveDate = audit.Timestamp;
            category.UpdateDate = audit.Timestamp;
            category.UpdaterId = audit.UserId;
            category.Ip = audit.IpAddress;

            await _categoryRepository.UpdateAsync(category, cancellationToken);

            return Result.Success();
        }
    }
}

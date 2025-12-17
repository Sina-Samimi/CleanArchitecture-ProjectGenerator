using System;
using System.Threading;
using System.Threading.Tasks;
using LogsDtoCloneTest.Application.Abstractions.Messaging;
using LogsDtoCloneTest.Application.Interfaces;
using LogsDtoCloneTest.SharedKernel.BaseTypes;

namespace LogsDtoCloneTest.Application.Commands.Catalog;

public sealed record DeleteProductCommand(Guid Id) : ICommand
{
    public sealed class Handler : ICommandHandler<DeleteProductCommand>
    {
        private readonly IProductRepository _productRepository;
        private readonly IAuditContext _auditContext;

        public Handler(IProductRepository productRepository, IAuditContext auditContext)
        {
            _productRepository = productRepository;
            _auditContext = auditContext;
        }

        public async Task<Result> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
        {
            var product = await _productRepository.GetByIdAsync(request.Id, cancellationToken);
            if (product is null)
            {
                return Result.Failure("محصول مورد نظر یافت نشد.");
            }

            if (product.IsDeleted)
            {
                return Result.Success();
            }

            var audit = _auditContext.Capture();

            product.IsDeleted = true;
            product.RemoveDate = audit.Timestamp;
            product.UpdateDate = audit.Timestamp;
            product.UpdaterId = audit.UserId;
            product.Ip = audit.IpAddress;

            await _productRepository.UpdateAsync(product, cancellationToken);

            return Result.Success();
        }
    }
}

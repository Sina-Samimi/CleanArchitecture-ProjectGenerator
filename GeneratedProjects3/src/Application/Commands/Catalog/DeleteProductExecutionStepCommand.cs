using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LogTableRenameTest.Application.Abstractions.Messaging;
using LogTableRenameTest.Application.Interfaces;
using LogTableRenameTest.SharedKernel.BaseTypes;

namespace LogTableRenameTest.Application.Commands.Catalog;

public sealed record DeleteProductExecutionStepCommand(Guid ProductId, Guid StepId) : ICommand
{
    public sealed class Handler : ICommandHandler<DeleteProductExecutionStepCommand>
    {
        private readonly IProductRepository _productRepository;
        private readonly IAuditContext _auditContext;

        public Handler(IProductRepository productRepository, IAuditContext auditContext)
        {
            _productRepository = productRepository;
            _auditContext = auditContext;
        }

        public async Task<Result> Handle(DeleteProductExecutionStepCommand request, CancellationToken cancellationToken)
        {
            var product = await _productRepository.GetWithDetailsAsync(request.ProductId, cancellationToken);
            if (product is null)
            {
                return Result.Failure("محصول مورد نظر یافت نشد.");
            }

            var step = product.ExecutionSteps.FirstOrDefault(item => item.Id == request.StepId && !item.IsDeleted);
            if (step is null)
            {
                return Result.Failure("گام انتخاب شده یافت نشد.");
            }

            var audit = _auditContext.Capture();

            step.UpdaterId = audit.UserId;
            step.UpdateDate = audit.Timestamp;
            step.RemoveDate = audit.Timestamp;
            step.IsDeleted = true;
            step.Ip = audit.IpAddress;

            var removed = product.RemoveExecutionStep(request.StepId);
            if (!removed)
            {
                return Result.Failure("حذف گام ممکن نشد.");
            }

            product.UpdaterId = audit.UserId;
            product.UpdateDate = audit.Timestamp;
            product.Ip = audit.IpAddress;

            await _productRepository.UpdateAsync(product, cancellationToken);

            return Result.Success();
        }
    }
}

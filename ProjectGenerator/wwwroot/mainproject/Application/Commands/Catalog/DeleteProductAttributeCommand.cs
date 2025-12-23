using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MobiRooz.Application.Abstractions.Messaging;
using MobiRooz.Application.Interfaces;
using MobiRooz.SharedKernel.BaseTypes;

namespace MobiRooz.Application.Commands.Catalog;

public sealed record DeleteProductAttributeCommand(Guid ProductId, Guid AttributeId) : ICommand
{
    public sealed class Handler : ICommandHandler<DeleteProductAttributeCommand>
    {
        private readonly IProductRepository _productRepository;
        private readonly IAuditContext _auditContext;

        public Handler(IProductRepository productRepository, IAuditContext auditContext)
        {
            _productRepository = productRepository;
            _auditContext = auditContext;
        }

        public async Task<Result> Handle(DeleteProductAttributeCommand request, CancellationToken cancellationToken)
        {
            var product = await _productRepository.GetWithDetailsAsync(request.ProductId, cancellationToken);
            if (product is null)
            {
                return Result.Failure("محصول مورد نظر یافت نشد.");
            }

            var attribute = product.Attributes.FirstOrDefault(item => item.Id == request.AttributeId && !item.IsDeleted);
            if (attribute is null)
            {
                return Result.Failure("ویژگی انتخاب شده یافت نشد.");
            }

            var audit = _auditContext.Capture();

            attribute.UpdaterId = audit.UserId;
            attribute.UpdateDate = audit.Timestamp;
            attribute.RemoveDate = audit.Timestamp;
            attribute.IsDeleted = true;
            attribute.Ip = audit.IpAddress;

            var removed = product.RemoveAttribute(request.AttributeId);
            if (!removed)
            {
                return Result.Failure("حذف ویژگی ممکن نشد.");
            }

            product.UpdaterId = audit.UserId;
            product.UpdateDate = audit.Timestamp;

            await _productRepository.UpdateAsync(product, cancellationToken);

            return Result.Success();
        }
    }
}


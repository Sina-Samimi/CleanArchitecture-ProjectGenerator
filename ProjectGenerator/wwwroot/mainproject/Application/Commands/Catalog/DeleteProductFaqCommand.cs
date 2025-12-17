using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Attar.Application.Abstractions.Messaging;
using Attar.Application.Interfaces;
using Attar.SharedKernel.BaseTypes;

namespace Attar.Application.Commands.Catalog;

public sealed record DeleteProductFaqCommand(Guid ProductId, Guid FaqId) : ICommand
{
    public sealed class Handler : ICommandHandler<DeleteProductFaqCommand>
    {
        private readonly IProductRepository _productRepository;
        private readonly IAuditContext _auditContext;

        public Handler(IProductRepository productRepository, IAuditContext auditContext)
        {
            _productRepository = productRepository;
            _auditContext = auditContext;
        }

        public async Task<Result> Handle(DeleteProductFaqCommand request, CancellationToken cancellationToken)
        {
            var product = await _productRepository.GetWithDetailsAsync(request.ProductId, cancellationToken);
            if (product is null)
            {
                return Result.Failure("محصول مورد نظر یافت نشد.");
            }

            var faq = product.Faqs.FirstOrDefault(item => item.Id == request.FaqId && !item.IsDeleted);
            if (faq is null)
            {
                return Result.Failure("سوال انتخاب شده یافت نشد.");
            }

            var audit = _auditContext.Capture();

            faq.UpdaterId = audit.UserId;
            faq.UpdateDate = audit.Timestamp;
            faq.RemoveDate = audit.Timestamp;
            faq.IsDeleted = true;
            faq.Ip = audit.IpAddress;

            var removed = product.RemoveFaq(request.FaqId);
            if (!removed)
            {
                return Result.Failure("حذف سوال ممکن نشد.");
            }

            product.UpdaterId = audit.UserId;
            product.UpdateDate = audit.Timestamp;
            product.Ip = audit.IpAddress;

            await _productRepository.UpdateAsync(product, cancellationToken);

            return Result.Success();
        }
    }
}

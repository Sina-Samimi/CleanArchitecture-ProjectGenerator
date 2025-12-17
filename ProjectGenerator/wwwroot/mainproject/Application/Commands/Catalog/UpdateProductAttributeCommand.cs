using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Attar.Application.Abstractions.Messaging;
using Attar.Application.Interfaces;
using Attar.SharedKernel.BaseTypes;

namespace Attar.Application.Commands.Catalog;

public sealed record UpdateProductAttributeCommand(
    Guid ProductId,
    Guid AttributeId,
    string Key,
    string Value,
    int DisplayOrder) : ICommand
{
    private const int MaxKeyLength = 200;
    private const int MaxValueLength = 500;

    public sealed class Handler : ICommandHandler<UpdateProductAttributeCommand>
    {
        private readonly IProductRepository _productRepository;
        private readonly IAuditContext _auditContext;

        public Handler(IProductRepository productRepository, IAuditContext auditContext)
        {
            _productRepository = productRepository;
            _auditContext = auditContext;
        }

        public async Task<Result> Handle(UpdateProductAttributeCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Key))
            {
                return Result.Failure("کلید ویژگی الزامی است.");
            }

            if (string.IsNullOrWhiteSpace(request.Value))
            {
                return Result.Failure("مقدار ویژگی الزامی است.");
            }

            var key = request.Key.Trim();
            if (key.Length > MaxKeyLength)
            {
                return Result.Failure($"کلید ویژگی نمی‌تواند بیش از {MaxKeyLength} کاراکتر باشد.");
            }

            var value = request.Value.Trim();
            if (value.Length > MaxValueLength)
            {
                return Result.Failure($"مقدار ویژگی نمی‌تواند بیش از {MaxValueLength} کاراکتر باشد.");
            }

            if (request.DisplayOrder < 0)
            {
                return Result.Failure("ترتیب نمایش نمی‌تواند منفی باشد.");
            }

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

            attribute.UpdateContent(key, value);
            attribute.SetDisplayOrder(request.DisplayOrder);
            attribute.UpdaterId = audit.UserId;
            attribute.UpdateDate = audit.Timestamp;
            attribute.Ip = audit.IpAddress;

            product.UpdaterId = audit.UserId;
            product.UpdateDate = audit.Timestamp;

            await _productRepository.UpdateAsync(product, cancellationToken);

            return Result.Success();
        }
    }
}


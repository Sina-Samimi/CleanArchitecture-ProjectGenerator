using System;
using System.Threading;
using System.Threading.Tasks;
using Attar.Application.Abstractions.Messaging;
using Attar.Application.Interfaces;
using Attar.SharedKernel.BaseTypes;

namespace Attar.Application.Commands.Catalog;

public sealed record CreateProductAttributeCommand(
    Guid ProductId,
    string Key,
    string Value,
    int DisplayOrder) : ICommand<Guid>
{
    private const int MaxKeyLength = 200;
    private const int MaxValueLength = 500;

    public sealed class Handler : ICommandHandler<CreateProductAttributeCommand, Guid>
    {
        private readonly IProductRepository _productRepository;
        private readonly IAuditContext _auditContext;

        public Handler(IProductRepository productRepository, IAuditContext auditContext)
        {
            _productRepository = productRepository;
            _auditContext = auditContext;
        }

        public async Task<Result<Guid>> Handle(CreateProductAttributeCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Key))
            {
                return Result<Guid>.Failure("کلید ویژگی الزامی است.");
            }

            if (string.IsNullOrWhiteSpace(request.Value))
            {
                return Result<Guid>.Failure("مقدار ویژگی الزامی است.");
            }

            var key = request.Key.Trim();
            if (key.Length > MaxKeyLength)
            {
                return Result<Guid>.Failure($"کلید ویژگی نمی‌تواند بیش از {MaxKeyLength} کاراکتر باشد.");
            }

            var value = request.Value.Trim();
            if (value.Length > MaxValueLength)
            {
                return Result<Guid>.Failure($"مقدار ویژگی نمی‌تواند بیش از {MaxValueLength} کاراکتر باشد.");
            }

            if (request.DisplayOrder < 0)
            {
                return Result<Guid>.Failure("ترتیب نمایش نمی‌تواند منفی باشد.");
            }

            var product = await _productRepository.GetWithDetailsAsync(request.ProductId, cancellationToken);
            if (product is null)
            {
                return Result<Guid>.Failure("محصول مورد نظر یافت نشد.");
            }

            var audit = _auditContext.Capture();

            var attribute = product.AddAttribute(key, value, request.DisplayOrder);
            attribute.CreatorId = audit.UserId;
            attribute.CreateDate = audit.Timestamp;
            attribute.UpdateDate = audit.Timestamp;
            attribute.Ip = audit.IpAddress;

            product.UpdaterId = audit.UserId;
            product.UpdateDate = audit.Timestamp;

            await _productRepository.UpdateAsync(product, cancellationToken);

            return Result<Guid>.Success(attribute.Id);
        }
    }
}


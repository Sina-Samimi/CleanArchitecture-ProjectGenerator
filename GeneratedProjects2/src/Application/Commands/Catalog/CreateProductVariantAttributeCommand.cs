using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LogsDtoCloneTest.Application.Abstractions.Messaging;
using LogsDtoCloneTest.Application.Interfaces;
using LogsDtoCloneTest.SharedKernel.BaseTypes;

namespace LogsDtoCloneTest.Application.Commands.Catalog;

public sealed record CreateProductVariantAttributeCommand(
    Guid ProductId,
    string Name,
    IReadOnlyCollection<string>? Options = null,
    int DisplayOrder = 0) : ICommand<Guid>;

public sealed class CreateProductVariantAttributeCommandHandler : ICommandHandler<CreateProductVariantAttributeCommand, Guid>
{
    private readonly IProductRepository _productRepository;
    private readonly IAuditContext _auditContext;

    public CreateProductVariantAttributeCommandHandler(
        IProductRepository productRepository,
        IAuditContext auditContext)
    {
        _productRepository = productRepository;
        _auditContext = auditContext;
    }

    public async Task<Result<Guid>> Handle(CreateProductVariantAttributeCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return Result<Guid>.Failure("نام attribute الزامی است.");
        }

        var product = await _productRepository.GetWithDetailsAsync(request.ProductId, cancellationToken);
        if (product is null)
        {
            return Result<Guid>.Failure("محصول مورد نظر یافت نشد.");
        }

        var attribute = product.AddVariantAttribute(request.Name.Trim(), request.Options, request.DisplayOrder);

        var audit = _auditContext.Capture();
        attribute.CreatorId = audit.UserId;
        attribute.CreateDate = audit.Timestamp;
        attribute.UpdateDate = audit.Timestamp;
        attribute.Ip = audit.IpAddress;

        await _productRepository.UpdateAsync(product, cancellationToken);

        return Result<Guid>.Success(attribute.Id);
    }
}

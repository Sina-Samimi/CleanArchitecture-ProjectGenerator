using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LogsDtoCloneTest.Application.Abstractions.Messaging;
using LogsDtoCloneTest.Application.Interfaces;
using LogsDtoCloneTest.SharedKernel.BaseTypes;

namespace LogsDtoCloneTest.Application.Commands.Catalog;

public sealed record UpdateProductVariantAttributeCommand(
    Guid ProductId,
    Guid AttributeId,
    string Name,
    IReadOnlyCollection<string>? Options = null,
    int DisplayOrder = 0) : ICommand;

public sealed class UpdateProductVariantAttributeCommandHandler : ICommandHandler<UpdateProductVariantAttributeCommand>
{
    private readonly IProductRepository _productRepository;

    public UpdateProductVariantAttributeCommandHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<Result> Handle(UpdateProductVariantAttributeCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return Result.Failure("نام attribute الزامی است.");
        }

        var product = await _productRepository.GetWithDetailsAsync(request.ProductId, cancellationToken);
        if (product is null)
        {
            return Result.Failure("محصول مورد نظر یافت نشد.");
        }

        var attribute = product.VariantAttributes.FirstOrDefault(a => a.Id == request.AttributeId);
        if (attribute is null)
        {
            return Result.Failure("Attribute مورد نظر یافت نشد.");
        }

        attribute.UpdateName(request.Name.Trim());
        attribute.SetOptions(request.Options);
        attribute.SetDisplayOrder(request.DisplayOrder);

        await _productRepository.UpdateAsync(product, cancellationToken);

        return Result.Success();
    }
}

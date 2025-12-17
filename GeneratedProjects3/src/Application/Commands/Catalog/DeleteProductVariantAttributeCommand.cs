using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LogTableRenameTest.Application.Abstractions.Messaging;
using LogTableRenameTest.Application.Interfaces;
using LogTableRenameTest.SharedKernel.BaseTypes;

namespace LogTableRenameTest.Application.Commands.Catalog;

public sealed record DeleteProductVariantAttributeCommand(
    Guid ProductId,
    Guid AttributeId) : ICommand;

public sealed class DeleteProductVariantAttributeCommandHandler : ICommandHandler<DeleteProductVariantAttributeCommand>
{
    private readonly IProductRepository _productRepository;

    public DeleteProductVariantAttributeCommandHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<Result> Handle(DeleteProductVariantAttributeCommand request, CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetWithDetailsAsync(request.ProductId, cancellationToken);
        if (product is null)
        {
            return Result.Failure("محصول مورد نظر یافت نشد.");
        }

        var removed = product.RemoveVariantAttribute(request.AttributeId);
        if (!removed)
        {
            return Result.Failure("Attribute مورد نظر یافت نشد.");
        }

        await _productRepository.UpdateAsync(product, cancellationToken);

        return Result.Success();
    }
}

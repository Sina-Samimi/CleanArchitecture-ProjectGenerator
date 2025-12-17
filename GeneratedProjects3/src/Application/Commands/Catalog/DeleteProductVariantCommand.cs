using System;
using System.Threading;
using System.Threading.Tasks;
using LogTableRenameTest.Application.Abstractions.Messaging;
using LogTableRenameTest.Application.Interfaces;
using LogTableRenameTest.SharedKernel.BaseTypes;

namespace LogTableRenameTest.Application.Commands.Catalog;

public sealed record DeleteProductVariantCommand(
    Guid ProductId,
    Guid VariantId) : ICommand;

public sealed class DeleteProductVariantCommandHandler : ICommandHandler<DeleteProductVariantCommand>
{
    private readonly IProductRepository _productRepository;

    public DeleteProductVariantCommandHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<Result> Handle(DeleteProductVariantCommand request, CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetWithDetailsAsync(request.ProductId, cancellationToken);
        if (product is null)
        {
            return Result.Failure("محصول مورد نظر یافت نشد.");
        }

        var removed = product.RemoveVariant(request.VariantId);
        if (!removed)
        {
            return Result.Failure("Variant مورد نظر یافت نشد.");
        }

        await _productRepository.UpdateAsync(product, cancellationToken);

        return Result.Success();
    }
}

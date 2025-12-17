using System;
using System.Threading;
using System.Threading.Tasks;
using LogsDtoCloneTest.Application.Abstractions.Messaging;
using LogsDtoCloneTest.Application.Interfaces;
using LogsDtoCloneTest.SharedKernel.BaseTypes;

namespace LogsDtoCloneTest.Application.Commands.Catalog;

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

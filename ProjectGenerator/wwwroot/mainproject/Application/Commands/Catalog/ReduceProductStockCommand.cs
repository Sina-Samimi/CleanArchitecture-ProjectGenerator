using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Attar.Application.Abstractions.Messaging;
using Attar.Application.Interfaces;
using Attar.Domain.Entities.Catalog;
using Attar.Domain.Exceptions;
using Attar.SharedKernel.BaseTypes;
using Microsoft.Extensions.Logging;

namespace Attar.Application.Commands.Catalog;

public sealed record ReduceProductStockCommand(
    Guid ProductId,
    Guid? VariantId,
    int Quantity) : ICommand<bool>
{
    public sealed class Handler : ICommandHandler<ReduceProductStockCommand, bool>
    {
        private readonly IProductRepository _productRepository;
        private readonly ILogger<Handler> _logger;

        public Handler(IProductRepository productRepository, ILogger<Handler> logger)
        {
            _productRepository = productRepository;
            _logger = logger;
        }

        public async Task<Result<bool>> Handle(ReduceProductStockCommand request, CancellationToken cancellationToken)
        {
            if (request.ProductId == Guid.Empty)
            {
                return Result<bool>.Failure("شناسه محصول معتبر نیست.");
            }

            if (request.Quantity <= 0)
            {
                return Result<bool>.Failure("تعداد باید بزرگتر از صفر باشد.");
            }

            // Use GetWithDetailsAsync to ensure product is tracked for variant changes
            var product = await _productRepository.GetWithDetailsAsync(request.ProductId, cancellationToken);
            if (product is null)
            {
                _logger.LogWarning("ReduceProductStockCommand - Product not found: {ProductId}", request.ProductId);
                return Result<bool>.Failure("محصول مورد نظر یافت نشد.");
            }

            try
            {
                if (request.VariantId.HasValue && request.VariantId.Value != Guid.Empty)
                {
                    // Reduce variant stock
                    var variant = product.Variants.FirstOrDefault(v => v.Id == request.VariantId.Value);
                    if (variant is null)
                    {
                        _logger.LogWarning("ReduceProductStockCommand - Variant not found: {ProductId}, {VariantId}", 
                            request.ProductId, request.VariantId);
                        return Result<bool>.Failure("Variant مورد نظر یافت نشد.");
                    }

                    var oldVariantStock = variant.StockQuantity;
                    var oldProductStock = product.StockQuantity;

                    // If variant has its own stock (stock > 0), reduce variant stock
                    // If variant stock is 0, it uses product stock, so reduce product stock
                    if (variant.StockQuantity > 0)
                    {
                        variant.ReduceStock(request.Quantity);
                        _logger.LogInformation("ReduceProductStockCommand - Reduced variant stock: ProductId={ProductId}, VariantId={VariantId}, OldStock={OldStock}, NewStock={NewStock}, Quantity={Quantity}", 
                            request.ProductId, request.VariantId, oldVariantStock, variant.StockQuantity, request.Quantity);
                    }
                    else if (product.TrackInventory)
                    {
                        // Variant uses product stock
                        product.ReduceStock(request.Quantity);
                        _logger.LogInformation("ReduceProductStockCommand - Reduced product stock (variant uses product stock): ProductId={ProductId}, VariantId={VariantId}, OldStock={OldStock}, NewStock={NewStock}, Quantity={Quantity}", 
                            request.ProductId, request.VariantId, oldProductStock, product.StockQuantity, request.Quantity);
                    }
                }
                else
                {
                    // Reduce product stock
                    if (product.TrackInventory)
                    {
                        var oldStock = product.StockQuantity;
                        product.ReduceStock(request.Quantity);
                        _logger.LogInformation("ReduceProductStockCommand - Reduced product stock: ProductId={ProductId}, OldStock={OldStock}, NewStock={NewStock}, Quantity={Quantity}", 
                            request.ProductId, oldStock, product.StockQuantity, request.Quantity);
                    }
                }

                await _productRepository.UpdateAsync(product, cancellationToken);
                _logger.LogInformation("ReduceProductStockCommand - Successfully updated product: {ProductId}", request.ProductId);

                return Result<bool>.Success(true);
            }
            catch (InvalidOperationException ex)
            {
                return Result<bool>.Failure(ex.Message);
            }
            catch (DomainException ex)
            {
                return Result<bool>.Failure(ex.Message);
            }
        }
    }
}

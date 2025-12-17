using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LogsDtoCloneTest.Application.Abstractions.Messaging;
using LogsDtoCloneTest.Application.Interfaces;
using LogsDtoCloneTest.SharedKernel.BaseTypes;

namespace LogsDtoCloneTest.Application.Commands.Catalog;

public sealed record UpdateProductVariantCommand(
    Guid ProductId,
    Guid VariantId,
    decimal? Price = null,
    decimal? CompareAtPrice = null,
    int StockQuantity = 0,
    string? Sku = null,
    string? ImagePath = null,
    bool IsActive = true,
    IReadOnlyCollection<UpdateProductVariantCommand.VariantOption>? Options = null) : ICommand
{
    public sealed record VariantOption(Guid VariantAttributeId, string Value);
}

public sealed class UpdateProductVariantCommandHandler : ICommandHandler<UpdateProductVariantCommand>
{
    private readonly IProductRepository _productRepository;

    public UpdateProductVariantCommandHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<Result> Handle(UpdateProductVariantCommand request, CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetWithDetailsAsync(request.ProductId, cancellationToken);
        if (product is null)
        {
            return Result.Failure("محصول مورد نظر یافت نشد.");
        }

        var variant = product.GetVariantById(request.VariantId);
        if (variant is null)
        {
            return Result.Failure("Variant مورد نظر یافت نشد.");
        }

        // Validate options if provided
        if (request.Options is not null && request.Options.Count > 0)
        {
            var attributeIds = product.VariantAttributes.Select(a => a.Id).ToHashSet();
            var invalidOptions = request.Options
                .Where(opt => !attributeIds.Contains(opt.VariantAttributeId))
                .ToList();

            if (invalidOptions.Count > 0)
            {
                return Result.Failure($"Variant attribute های نامعتبر: {string.Join(", ", invalidOptions.Select(o => o.VariantAttributeId))}");
            }

            // Check for duplicate attribute IDs
            var duplicateAttributes = request.Options
                .GroupBy(o => o.VariantAttributeId)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicateAttributes.Count > 0)
            {
                return Result.Failure("هر variant attribute فقط یک بار می‌تواند استفاده شود.");
            }
        }

        variant.SetPricing(request.Price, request.CompareAtPrice);
        variant.SetStockQuantity(request.StockQuantity);
        variant.SetSku(request.Sku);
        variant.SetImagePath(request.ImagePath);
        variant.SetActive(request.IsActive);

        // Update options
        if (request.Options is not null)
        {
            var optionsList = request.Options
                .Select(opt => (opt.VariantAttributeId, opt.Value))
                .ToList();
            variant.SetOptions(optionsList);
        }

        await _productRepository.UpdateAsync(product, cancellationToken);

        return Result.Success();
    }
}

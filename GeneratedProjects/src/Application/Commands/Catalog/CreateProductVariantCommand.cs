using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TestAttarClone.Application.Abstractions.Messaging;
using TestAttarClone.Application.Interfaces;
using TestAttarClone.SharedKernel.BaseTypes;

namespace TestAttarClone.Application.Commands.Catalog;

public sealed record CreateProductVariantCommand(
    Guid ProductId,
    decimal? Price = null,
    decimal? CompareAtPrice = null,
    int StockQuantity = 0,
    string? Sku = null,
    string? ImagePath = null,
    bool IsActive = true,
    IReadOnlyCollection<CreateProductVariantCommand.VariantOption>? Options = null) : ICommand<Guid>
{
    public sealed record VariantOption(Guid VariantAttributeId, string Value);
}

public sealed class CreateProductVariantCommandHandler : ICommandHandler<CreateProductVariantCommand, Guid>
{
    private readonly IProductRepository _productRepository;
    private readonly IAuditContext _auditContext;

    public CreateProductVariantCommandHandler(
        IProductRepository productRepository,
        IAuditContext auditContext)
    {
        _productRepository = productRepository;
        _auditContext = auditContext;
    }

    public async Task<Result<Guid>> Handle(CreateProductVariantCommand request, CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetWithDetailsAsync(request.ProductId, cancellationToken);
        if (product is null)
        {
            return Result<Guid>.Failure("محصول مورد نظر یافت نشد.");
        }

        // Validate that all variant attribute IDs exist
        if (request.Options is not null && request.Options.Count > 0)
        {
            var attributeIds = product.VariantAttributes.Select(a => a.Id).ToHashSet();
            var invalidOptions = request.Options
                .Where(opt => !attributeIds.Contains(opt.VariantAttributeId))
                .ToList();

            if (invalidOptions.Count > 0)
            {
                return Result<Guid>.Failure($"Variant attribute های نامعتبر: {string.Join(", ", invalidOptions.Select(o => o.VariantAttributeId))}");
            }

            // Check for duplicate attribute IDs
            var duplicateAttributes = request.Options
                .GroupBy(o => o.VariantAttributeId)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicateAttributes.Count > 0)
            {
                return Result<Guid>.Failure("هر variant attribute فقط یک بار می‌تواند استفاده شود.");
            }
        }

        var variant = product.AddVariant(
            request.Price,
            request.CompareAtPrice,
            request.StockQuantity,
            request.Sku,
            request.ImagePath,
            request.IsActive);

        // Add options to variant
        if (request.Options is not null && request.Options.Count > 0)
        {
            var optionsList = request.Options
                .Select(opt => (opt.VariantAttributeId, opt.Value))
                .ToList();
            variant.SetOptions(optionsList);
        }

        var audit = _auditContext.Capture();
        variant.CreatorId = audit.UserId;
        variant.CreateDate = audit.Timestamp;
        variant.UpdateDate = audit.Timestamp;
        variant.Ip = audit.IpAddress;

        await _productRepository.UpdateAsync(product, cancellationToken);

        return Result<Guid>.Success(variant.Id);
    }
}

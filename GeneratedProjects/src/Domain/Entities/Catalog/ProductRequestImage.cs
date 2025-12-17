using System;
using System.Diagnostics.CodeAnalysis;
using TestAttarClone.Domain.Base;

namespace TestAttarClone.Domain.Entities.Catalog;

public sealed class ProductRequestImage : Entity
{
    public Guid ProductRequestId { get; private set; }

    public ProductRequest ProductRequest { get; private set; } = null!;

    public string Path { get; private set; }

    public int Order { get; private set; }

    [SetsRequiredMembers]
    private ProductRequestImage()
    {
        Path = string.Empty;
        ProductRequestId = Guid.Empty;
    }

    [SetsRequiredMembers]
    public ProductRequestImage(Guid productRequestId, string path, int order)
    {
        if (productRequestId == Guid.Empty)
        {
            throw new ArgumentException("Product request ID cannot be empty", nameof(productRequestId));
        }

        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Image path cannot be empty", nameof(path));
        }

        ProductRequestId = productRequestId;
        Path = path.Trim();
        Order = order;
    }
}


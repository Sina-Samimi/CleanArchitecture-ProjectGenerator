using System;
using System.Diagnostics.CodeAnalysis;
using LogsDtoCloneTest.Domain.Base;
using LogsDtoCloneTest.Domain.Exceptions;

namespace LogsDtoCloneTest.Domain.Entities.Catalog;

public sealed class Wishlist : Entity
{
    [SetsRequiredMembers]
    private Wishlist()
    {
        UserId = string.Empty;
    }

    [SetsRequiredMembers]
    public Wishlist(string userId, Guid productId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new DomainException("شناسه کاربر برای علاقه‌مندی الزامی است.");
        }

        if (productId == Guid.Empty)
        {
            throw new DomainException("شناسه محصول برای علاقه‌مندی الزامی است.");
        }

        UserId = userId.Trim();
        ProductId = productId;
    }

    public string UserId { get; private set; }

    public Guid ProductId { get; private set; }

    public Product Product { get; private set; } = null!;
}


using System;
using System.Collections.Generic;
using System.Linq;
using Arsis.Domain.Entities.Orders;

namespace Arsis.Application.DTOs.Cart;

public static class CartDtoMapper
{
    public static CartDto ToDto(this ShoppingCart cart)
    {
        ArgumentNullException.ThrowIfNull(cart);

        var items = cart.Items
            .OrderByDescending(item => item.UpdateDate)
            .ThenBy(item => item.ProductName)
            .Select(item => new CartItemDto(
                item.ProductId,
                item.ProductName,
                item.ProductSlug,
                item.ThumbnailPath,
                item.UnitPrice,
                item.CompareAtPrice,
                item.Quantity,
                item.ProductType,
                item.LineTotal))
            .ToList();

        CartDiscountDto? discount = null;
        if (cart.HasDiscount
            && !string.IsNullOrWhiteSpace(cart.AppliedDiscountCode)
            && cart.AppliedDiscountType is not null
            && cart.AppliedDiscountValue is not null
            && cart.AppliedDiscountAmount is not null
            && cart.DiscountEvaluatedAt is not null)
        {
            discount = new CartDiscountDto(
                cart.AppliedDiscountCode!,
                cart.AppliedDiscountType!.Value,
                cart.AppliedDiscountValue!.Value,
                cart.AppliedDiscountAmount!.Value,
                cart.AppliedDiscountWasCapped,
                cart.DiscountEvaluatedAt!.Value);
        }

        return new CartDto(
            cart.Id,
            cart.AnonymousId,
            cart.UserId,
            items,
            cart.Subtotal,
            cart.DiscountTotal,
            cart.GrandTotal,
            discount);
    }

    public static CartDto CreateEmpty(Guid? anonymousId, string? userId)
        => new(
            Guid.Empty,
            anonymousId,
            userId,
            Array.Empty<CartItemDto>(),
            0m,
            0m,
            0m,
            null);
}

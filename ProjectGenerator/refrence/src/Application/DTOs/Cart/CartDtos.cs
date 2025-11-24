using System;
using System.Collections.Generic;
using Arsis.Domain.Enums;

namespace Arsis.Application.DTOs.Cart;

public sealed record CartItemDto(
    Guid ProductId,
    string Name,
    string Slug,
    string? ThumbnailUrl,
    decimal UnitPrice,
    decimal? CompareAtPrice,
    int Quantity,
    ProductType ProductType,
    decimal LineTotal);

public sealed record CartDiscountDto(
    string Code,
    DiscountType DiscountType,
    decimal DiscountValue,
    decimal DiscountAmount,
    bool WasCapped,
    DateTimeOffset AppliedAt);

public sealed record CartDto(
    Guid Id,
    Guid? AnonymousId,
    string? UserId,
    IReadOnlyCollection<CartItemDto> Items,
    decimal Subtotal,
    decimal DiscountTotal,
    decimal GrandTotal,
    CartDiscountDto? Discount);

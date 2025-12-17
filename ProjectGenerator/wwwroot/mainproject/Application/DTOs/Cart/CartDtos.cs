using System;
using System.Collections.Generic;
using Attar.Domain.Enums;

namespace Attar.Application.DTOs.Cart;

public sealed record CartItemDto(
    Guid ProductId,
    Guid? OfferId,
    string Name,
    string Slug,
    string? ThumbnailUrl,
    decimal UnitPrice,
    decimal? CompareAtPrice,
    int Quantity,
    ProductType ProductType,
    decimal LineTotal,
    bool CanIncreaseQuantity);

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

public sealed record ActiveCartListItemDto(
    Guid Id,
    string? UserId,
    string? UserFullName,
    string? UserPhoneNumber,
    int ItemCount,
    decimal Subtotal,
    decimal DiscountTotal,
    decimal GrandTotal,
    DateTimeOffset UpdateDate,
    DateTimeOffset CreateDate);

public sealed record ActiveCartListResultDto(
    IReadOnlyList<ActiveCartListItemDto> Items,
    int TotalCount,
    int PageNumber,
    int PageSize,
    int TotalPages,
    DateTimeOffset QueryTimestamp);
using System;
using System.Collections.Generic;
using LogTableRenameTest.Domain.Enums;

namespace LogTableRenameTest.Application.DTOs.Orders;

public sealed record OrderDto(
    Guid InvoiceId,
    string InvoiceNumber,
    DateTimeOffset OrderDate,
    InvoiceStatus Status,
    string? UserId,
    Guid InvoiceItemId,
    string ProductName,
    decimal Quantity,
    decimal UnitPrice,
    decimal Total,
    Guid? ProductId,
    Guid? VariantId,
    string? SellerId,
    string? FullProductName);

public sealed record OrderListResultDto(
    IReadOnlyCollection<OrderDto> Items,
    int TotalCount,
    int PageNumber,
    int PageSize,
    DateTimeOffset GeneratedAt)
{
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}


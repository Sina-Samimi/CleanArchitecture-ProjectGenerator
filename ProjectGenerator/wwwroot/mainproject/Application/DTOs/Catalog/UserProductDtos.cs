using System;
using Attar.Domain.Enums;

namespace Attar.Application.DTOs.Catalog;

public sealed record UserPurchasedProductDto(
    Guid InvoiceId,
    string InvoiceNumber,
    Guid InvoiceItemId,
    Guid? ProductId,
    string Name,
    string? Summary,
    ProductType? ProductType,
    string? CategoryName,
    decimal Quantity,
    decimal UnitPrice,
    decimal Total,
    DateTimeOffset PurchasedAt,
    string? FeaturedImagePath,
    string? DigitalDownloadPath,
    InvoiceStatus InvoiceStatus,
    decimal InvoiceGrandTotal,
    decimal InvoicePaidAmount,
    decimal InvoiceOutstandingAmount);

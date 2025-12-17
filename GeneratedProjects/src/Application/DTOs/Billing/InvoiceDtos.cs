using System;
using System.Collections.Generic;
using System.Linq;
using TestAttarClone.Domain.Entities.Billing;
using TestAttarClone.Domain.Enums;

namespace TestAttarClone.Application.DTOs.Billing;

public sealed record InvoiceListItemDto(
    Guid Id,
    string InvoiceNumber,
    string Title,
    InvoiceStatus Status,
    string Currency,
    decimal GrandTotal,
    decimal PaidAmount,
    decimal OutstandingAmount,
    DateTimeOffset IssueDate,
    DateTimeOffset? DueDate,
    string? UserId,
    string? ExternalReference);

public sealed record InvoiceItemAttributeDto(
    Guid Id,
    string Key,
    string Value);

public sealed record InvoiceItemDto(
    Guid Id,
    string Name,
    string? Description,
    InvoiceItemType ItemType,
    Guid? ReferenceId,
    Guid? VariantId,
    decimal Quantity,
    decimal UnitPrice,
    decimal? DiscountAmount,
    decimal Subtotal,
    decimal Total,
    IReadOnlyCollection<InvoiceItemAttributeDto> Attributes);

public sealed record PaymentTransactionDto(
    Guid Id,
    decimal Amount,
    PaymentMethod Method,
    TransactionStatus Status,
    string Reference,
    string? GatewayName,
    string? Description,
    string? Metadata,
    DateTimeOffset OccurredAt);

public sealed record InvoiceDetailDto(
    Guid Id,
    string InvoiceNumber,
    string Title,
    string? Description,
    InvoiceStatus Status,
    string Currency,
    decimal Subtotal,
    decimal DiscountTotal,
    decimal TaxAmount,
    decimal AdjustmentAmount,
    decimal GrandTotal,
    decimal PaidAmount,
    decimal OutstandingAmount,
    DateTimeOffset IssueDate,
    DateTimeOffset? DueDate,
    string? UserId,
    string? ExternalReference,
    IReadOnlyCollection<InvoiceItemDto> Items,
    IReadOnlyCollection<PaymentTransactionDto> Transactions,
    Guid? ShippingAddressId = null,
    string? ShippingRecipientName = null,
    string? ShippingRecipientPhone = null,
    string? ShippingProvince = null,
    string? ShippingCity = null,
    string? ShippingPostalCode = null,
    string? ShippingAddressLine = null,
    string? ShippingPlaque = null,
    string? ShippingUnit = null);

public sealed record InvoiceSummaryMetricsDto(
    int TotalInvoices,
    int DraftInvoices,
    int PendingInvoices,
    int PaidInvoices,
    int PartiallyPaidInvoices,
    int CancelledInvoices,
    int OverdueInvoices,
    decimal TotalBilledAmount,
    decimal TotalOutstandingAmount,
    decimal TotalCollectedAmount);

public sealed record InvoiceListResultDto(
    IReadOnlyCollection<InvoiceListItemDto> Items,
    InvoiceSummaryMetricsDto Summary,
    DateTimeOffset GeneratedAt,
    int PageNumber = 1,
    int PageSize = 10,
    int TotalCount = 0)
{
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}

public sealed record InvoiceListFilterDto(
    string? SearchTerm,
    string? UserId,
    InvoiceStatus? Status,
    DateTimeOffset? IssueDateFrom,
    DateTimeOffset? IssueDateTo,
    int? PageNumber = null,
    int? PageSize = null);

public static class InvoiceDtoMapper
{
    public static InvoiceListItemDto ToListItemDto(this Invoice invoice)
        => new(
            invoice.Id,
            invoice.InvoiceNumber,
            invoice.Title,
            invoice.Status,
            invoice.Currency,
            invoice.GrandTotal,
            invoice.PaidAmount,
            invoice.OutstandingAmount,
            invoice.IssueDate,
            invoice.DueDate,
            invoice.UserId,
            invoice.ExternalReference);

    public static InvoiceDetailDto ToDetailDto(this Invoice invoice)
    {
        var itemDtos = invoice.Items
            .Select(item => new InvoiceItemDto(
                item.Id,
                item.Name,
                item.Description,
                item.ItemType,
                item.ReferenceId,
                item.VariantId,
                item.Quantity,
                item.UnitPrice,
                item.DiscountAmount,
                item.Subtotal,
                item.Total,
                item.Attributes
                    .Select(attribute => new InvoiceItemAttributeDto(attribute.Id, attribute.Key, attribute.Value))
                    .ToArray()))
            .ToArray();

        var transactionDtos = invoice.Transactions
            .OrderByDescending(transaction => transaction.OccurredAt)
            .Select(transaction => new PaymentTransactionDto(
                transaction.Id,
                transaction.Amount,
                transaction.Method,
                transaction.Status,
                transaction.Reference,
                transaction.GatewayName,
                transaction.Description,
                transaction.Metadata,
                transaction.OccurredAt))
            .ToArray();

        return new InvoiceDetailDto(
            invoice.Id,
            invoice.InvoiceNumber,
            invoice.Title,
            invoice.Description,
            invoice.Status,
            invoice.Currency,
            invoice.Subtotal,
            invoice.DiscountTotal,
            invoice.TaxAmount,
            invoice.AdjustmentAmount,
            invoice.GrandTotal,
            invoice.PaidAmount,
            invoice.OutstandingAmount,
            invoice.IssueDate,
            invoice.DueDate,
            invoice.UserId,
            invoice.ExternalReference,
            itemDtos,
            transactionDtos,
            invoice.ShippingAddressId,
            invoice.ShippingRecipientName,
            invoice.ShippingRecipientPhone,
            invoice.ShippingProvince,
            invoice.ShippingCity,
            invoice.ShippingPostalCode,
            invoice.ShippingAddressLine,
            invoice.ShippingPlaque,
            invoice.ShippingUnit);
    }

    public static InvoiceSummaryMetricsDto BuildSummary(this IEnumerable<Invoice> invoices)
    {
        var list = invoices.ToList();
        return new InvoiceSummaryMetricsDto(
            list.Count,
            list.Count(invoice => invoice.Status == InvoiceStatus.Draft),
            list.Count(invoice => invoice.Status == InvoiceStatus.Pending),
            list.Count(invoice => invoice.Status == InvoiceStatus.Paid),
            list.Count(invoice => invoice.Status == InvoiceStatus.PartiallyPaid),
            list.Count(invoice => invoice.Status == InvoiceStatus.Cancelled),
            list.Count(invoice => invoice.Status == InvoiceStatus.Overdue),
            list.Sum(invoice => invoice.GrandTotal),
            list.Sum(invoice => invoice.OutstandingAmount),
            list.Sum(invoice => invoice.PaidAmount));
    }
}

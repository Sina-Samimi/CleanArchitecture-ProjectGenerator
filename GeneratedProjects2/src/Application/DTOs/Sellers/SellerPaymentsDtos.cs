using System;
using System.Collections.Generic;

namespace LogsDtoCloneTest.Application.DTOs.Sellers;

public sealed record SellerPaymentsDto(
    decimal TotalRevenue,
    decimal PaidRevenue,
    decimal PendingRevenue,
    int TotalInvoices,
    int PaidInvoices,
    int PendingInvoices,
    IReadOnlyCollection<SellerInvoiceDto> Invoices,
    DateTimeOffset GeneratedAt);

public sealed record SellerInvoiceDto(
    Guid InvoiceId,
    string InvoiceNumber,
    string Title,
    decimal TotalAmount,
    decimal PaidAmount,
    decimal PendingAmount,
    string Status,
    DateTimeOffset IssueDate,
    DateTimeOffset? DueDate,
    int ProductCount);


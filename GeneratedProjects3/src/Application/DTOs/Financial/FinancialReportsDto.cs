using System;
using System.Collections.Generic;
using LogTableRenameTest.Domain.Enums;

namespace LogTableRenameTest.Application.DTOs.Financial;

public sealed record FinancialReportsDto(
    FinancialSummaryDto Summary,
    IReadOnlyCollection<DailySalesDto> DailySales,
    IReadOnlyCollection<TransactionStatusSummaryDto> TransactionStatusSummary,
    IReadOnlyCollection<PaymentMethodSummaryDto> PaymentMethodSummary,
    IReadOnlyCollection<InvoiceStatusSummaryDto> InvoiceStatusSummary,
    IReadOnlyCollection<MonthlyRevenueDto> MonthlyRevenue);

public sealed record FinancialSummaryDto(
    int TotalInvoices,
    int SuccessfulTransactions,
    int FailedTransactions,
    int PendingTransactions,
    decimal TotalRevenue,
    decimal TotalPaidAmount,
    decimal TotalOutstandingAmount,
    decimal AverageInvoiceAmount,
    decimal TotalRefundedAmount);

public sealed record DailySalesDto(
    DateOnly Date,
    int InvoiceCount,
    int SuccessfulTransactionCount,
    decimal TotalRevenue,
    decimal TotalPaidAmount);

public sealed record TransactionStatusSummaryDto(
    TransactionStatus Status,
    int Count,
    decimal TotalAmount);

public sealed record PaymentMethodSummaryDto(
    PaymentMethod Method,
    int TransactionCount,
    decimal TotalAmount,
    int SuccessfulCount,
    int FailedCount);

public sealed record InvoiceStatusSummaryDto(
    InvoiceStatus Status,
    int Count,
    decimal TotalAmount,
    decimal AverageAmount);

public sealed record MonthlyRevenueDto(
    int Year,
    int Month,
    int InvoiceCount,
    decimal TotalRevenue,
    decimal TotalPaidAmount,
    decimal TotalOutstandingAmount);


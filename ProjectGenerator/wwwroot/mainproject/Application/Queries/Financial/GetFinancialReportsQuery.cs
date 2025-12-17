using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Attar.Application.Abstractions.Messaging;
using Attar.Application.DTOs.Billing;
using Attar.Application.DTOs.Financial;
using Attar.Application.Interfaces;
using Attar.Domain.Enums;
using Attar.SharedKernel.BaseTypes;

namespace Attar.Application.Queries.Financial;

public sealed record GetFinancialReportsQuery(
    DateTimeOffset? FromDate = null,
    DateTimeOffset? ToDate = null) : IQuery<FinancialReportsDto>;

public sealed class GetFinancialReportsQueryHandler : IQueryHandler<GetFinancialReportsQuery, FinancialReportsDto>
{
    private readonly IInvoiceRepository _invoiceRepository;

    public GetFinancialReportsQueryHandler(IInvoiceRepository invoiceRepository)
    {
        _invoiceRepository = invoiceRepository;
    }

    public async Task<Result<FinancialReportsDto>> Handle(GetFinancialReportsQuery request, CancellationToken cancellationToken)
    {
        var fromDate = request.FromDate ?? DateTimeOffset.UtcNow.AddMonths(-12);
        var toDate = request.ToDate ?? DateTimeOffset.UtcNow;

        // Get all invoices in date range using repository
        var filter = new InvoiceListFilterDto(
            SearchTerm: null,
            UserId: null,
            Status: null,
            IssueDateFrom: fromDate,
            IssueDateTo: toDate,
            PageNumber: null,
            PageSize: null);

        var invoices = await _invoiceRepository.GetListAsync(filter, cancellationToken);

        // Get all payment transactions from invoices
        var allTransactions = invoices
            .SelectMany(i => i.Transactions)
            .ToList();

        // Summary
        var totalInvoices = invoices.Count;
        var successfulTransactions = allTransactions.Count(t => t.Status == TransactionStatus.Succeeded);
        var failedTransactions = allTransactions.Count(t => t.Status == TransactionStatus.Failed);
        var pendingTransactions = allTransactions.Count(t => t.Status == TransactionStatus.Pending);
        var totalRevenue = invoices.Sum(i => i.GrandTotal);
        var totalPaidAmount = invoices.Sum(i => i.PaidAmount);
        var totalOutstandingAmount = invoices.Sum(i => i.OutstandingAmount);
        var averageInvoiceAmount = totalInvoices > 0 ? totalRevenue / totalInvoices : 0;
        var totalRefundedAmount = allTransactions
            .Where(t => t.Status == TransactionStatus.Refunded)
            .Sum(t => t.Amount);

        var summary = new FinancialSummaryDto(
            totalInvoices,
            successfulTransactions,
            failedTransactions,
            pendingTransactions,
            totalRevenue,
            totalPaidAmount,
            totalOutstandingAmount,
            averageInvoiceAmount,
            totalRefundedAmount);

        // Daily Sales
        var dailySales = invoices
            .GroupBy(i => DateOnly.FromDateTime(i.IssueDate.Date))
            .Select(g => new DailySalesDto(
                g.Key,
                g.Count(),
                allTransactions
                    .Where(t => g.Select(i => i.Id).Contains(t.InvoiceId) && t.Status == TransactionStatus.Succeeded)
                    .Count(),
                g.Sum(i => i.GrandTotal),
                g.Sum(i => i.PaidAmount)))
            .OrderBy(d => d.Date)
            .ToList();

        // Transaction Status Summary
        var transactionStatusSummary = allTransactions
            .GroupBy(t => t.Status)
            .Select(g => new TransactionStatusSummaryDto(
                g.Key,
                g.Count(),
                g.Sum(t => t.Amount)))
            .ToList();

        // Payment Method Summary
        var paymentMethodSummary = allTransactions
            .GroupBy(t => t.Method)
            .Select(g => new PaymentMethodSummaryDto(
                g.Key,
                g.Count(),
                g.Sum(t => t.Amount),
                g.Count(t => t.Status == TransactionStatus.Succeeded),
                g.Count(t => t.Status == TransactionStatus.Failed)))
            .ToList();

        // Invoice Status Summary
        var invoiceStatusSummary = invoices
            .GroupBy(i => i.Status)
            .Select(g => new InvoiceStatusSummaryDto(
                g.Key,
                g.Count(),
                g.Sum(i => i.GrandTotal),
                g.Count() > 0 ? g.Average(i => i.GrandTotal) : 0))
            .ToList();

        // Monthly Revenue
        var monthlyRevenue = invoices
            .GroupBy(i => new { i.IssueDate.Year, i.IssueDate.Month })
            .Select(g => new MonthlyRevenueDto(
                g.Key.Year,
                g.Key.Month,
                g.Count(),
                g.Sum(i => i.GrandTotal),
                g.Sum(i => i.PaidAmount),
                g.Sum(i => i.OutstandingAmount)))
            .OrderBy(m => m.Year)
            .ThenBy(m => m.Month)
            .ToList();

        var result = new FinancialReportsDto(
            summary,
            dailySales,
            transactionStatusSummary,
            paymentMethodSummary,
            invoiceStatusSummary,
            monthlyRevenue);

        return Result<FinancialReportsDto>.Success(result);
    }
}


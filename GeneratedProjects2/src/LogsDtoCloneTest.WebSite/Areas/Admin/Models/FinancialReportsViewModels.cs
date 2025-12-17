using System;
using System.Collections.Generic;
using System.Linq;
using LogsDtoCloneTest.Application.DTOs.Financial;
using LogsDtoCloneTest.Domain.Enums;

namespace LogsDtoCloneTest.WebSite.Areas.Admin.Models;

public sealed class FinancialReportsViewModel
{
    public FinancialSummaryViewModel Summary { get; init; } = new();
    public IReadOnlyCollection<DailySalesViewModel> DailySales { get; init; } = Array.Empty<DailySalesViewModel>();
    public IReadOnlyCollection<TransactionStatusSummaryViewModel> TransactionStatusSummary { get; init; } = Array.Empty<TransactionStatusSummaryViewModel>();
    public IReadOnlyCollection<PaymentMethodSummaryViewModel> PaymentMethodSummary { get; init; } = Array.Empty<PaymentMethodSummaryViewModel>();
    public IReadOnlyCollection<InvoiceStatusSummaryViewModel> InvoiceStatusSummary { get; init; } = Array.Empty<InvoiceStatusSummaryViewModel>();
    public IReadOnlyCollection<MonthlyRevenueViewModel> MonthlyRevenue { get; init; } = Array.Empty<MonthlyRevenueViewModel>();

    public static FinancialReportsViewModel FromDto(FinancialReportsDto dto)
    {
        return new FinancialReportsViewModel
        {
            Summary = FinancialSummaryViewModel.FromDto(dto.Summary),
            DailySales = dto.DailySales.Select(DailySalesViewModel.FromDto).ToList(),
            TransactionStatusSummary = dto.TransactionStatusSummary.Select(TransactionStatusSummaryViewModel.FromDto).ToList(),
            PaymentMethodSummary = dto.PaymentMethodSummary.Select(PaymentMethodSummaryViewModel.FromDto).ToList(),
            InvoiceStatusSummary = dto.InvoiceStatusSummary.Select(InvoiceStatusSummaryViewModel.FromDto).ToList(),
            MonthlyRevenue = dto.MonthlyRevenue.Select(MonthlyRevenueViewModel.FromDto).ToList()
        };
    }

    public static FinancialReportsViewModel Empty()
    {
        return new FinancialReportsViewModel();
    }
}

public sealed class FinancialSummaryViewModel
{
    public int TotalInvoices { get; init; }
    public int SuccessfulTransactions { get; init; }
    public int FailedTransactions { get; init; }
    public int PendingTransactions { get; init; }
    public decimal TotalRevenue { get; init; }
    public decimal TotalPaidAmount { get; init; }
    public decimal TotalOutstandingAmount { get; init; }
    public decimal AverageInvoiceAmount { get; init; }
    public decimal TotalRefundedAmount { get; init; }

    public static FinancialSummaryViewModel FromDto(FinancialSummaryDto dto)
    {
        return new FinancialSummaryViewModel
        {
            TotalInvoices = dto.TotalInvoices,
            SuccessfulTransactions = dto.SuccessfulTransactions,
            FailedTransactions = dto.FailedTransactions,
            PendingTransactions = dto.PendingTransactions,
            TotalRevenue = dto.TotalRevenue,
            TotalPaidAmount = dto.TotalPaidAmount,
            TotalOutstandingAmount = dto.TotalOutstandingAmount,
            AverageInvoiceAmount = dto.AverageInvoiceAmount,
            TotalRefundedAmount = dto.TotalRefundedAmount
        };
    }
}

public sealed class DailySalesViewModel
{
    public DateOnly Date { get; init; }
    public int InvoiceCount { get; init; }
    public int SuccessfulTransactionCount { get; init; }
    public decimal TotalRevenue { get; init; }
    public decimal TotalPaidAmount { get; init; }

    public static DailySalesViewModel FromDto(DailySalesDto dto)
    {
        return new DailySalesViewModel
        {
            Date = dto.Date,
            InvoiceCount = dto.InvoiceCount,
            SuccessfulTransactionCount = dto.SuccessfulTransactionCount,
            TotalRevenue = dto.TotalRevenue,
            TotalPaidAmount = dto.TotalPaidAmount
        };
    }
}

public sealed class TransactionStatusSummaryViewModel
{
    public TransactionStatus Status { get; init; }
    public string StatusDisplay { get; init; } = string.Empty;
    public int Count { get; init; }
    public decimal TotalAmount { get; init; }

    public static TransactionStatusSummaryViewModel FromDto(TransactionStatusSummaryDto dto)
    {
        return new TransactionStatusSummaryViewModel
        {
            Status = dto.Status,
            StatusDisplay = dto.Status switch
            {
                TransactionStatus.Pending => "در انتظار",
                TransactionStatus.Succeeded => "موفق",
                TransactionStatus.Failed => "ناموفق",
                TransactionStatus.Cancelled => "لغو شده",
                TransactionStatus.Refunded => "عودت داده شده",
                _ => "نامشخص"
            },
            Count = dto.Count,
            TotalAmount = dto.TotalAmount
        };
    }
}

public sealed class PaymentMethodSummaryViewModel
{
    public PaymentMethod Method { get; init; }
    public string MethodDisplay { get; init; } = string.Empty;
    public int TransactionCount { get; init; }
    public decimal TotalAmount { get; init; }
    public int SuccessfulCount { get; init; }
    public int FailedCount { get; init; }
    public decimal SuccessRate => TransactionCount > 0 ? (decimal)SuccessfulCount / TransactionCount * 100 : 0;

    public static PaymentMethodSummaryViewModel FromDto(PaymentMethodSummaryDto dto)
    {
        return new PaymentMethodSummaryViewModel
        {
            Method = dto.Method,
            MethodDisplay = dto.Method switch
            {
                PaymentMethod.Unknown => "نامشخص",
                PaymentMethod.OnlineGateway => "درگاه آنلاین",
                PaymentMethod.BankTransfer => "کارت به کارت / حواله",
                PaymentMethod.Cash => "نقدی",
                PaymentMethod.Wallet => "کیف پول",
                _ => "نامشخص"
            },
            TransactionCount = dto.TransactionCount,
            TotalAmount = dto.TotalAmount,
            SuccessfulCount = dto.SuccessfulCount,
            FailedCount = dto.FailedCount
        };
    }
}

public sealed class InvoiceStatusSummaryViewModel
{
    public InvoiceStatus Status { get; init; }
    public string StatusDisplay { get; init; } = string.Empty;
    public int Count { get; init; }
    public decimal TotalAmount { get; init; }
    public decimal AverageAmount { get; init; }

    public static InvoiceStatusSummaryViewModel FromDto(InvoiceStatusSummaryDto dto)
    {
        return new InvoiceStatusSummaryViewModel
        {
            Status = dto.Status,
            StatusDisplay = dto.Status switch
            {
                InvoiceStatus.Draft => "پیش‌نویس",
                InvoiceStatus.Pending => "در انتظار پرداخت",
                InvoiceStatus.Paid => "تسویه شده",
                InvoiceStatus.PartiallyPaid => "تسویه جزئی",
                InvoiceStatus.Cancelled => "لغو شده",
                InvoiceStatus.Overdue => "سررسید گذشته",
                _ => "نامشخص"
            },
            Count = dto.Count,
            TotalAmount = dto.TotalAmount,
            AverageAmount = dto.AverageAmount
        };
    }
}

public sealed class MonthlyRevenueViewModel
{
    public int Year { get; init; }
    public int Month { get; init; }
    public string MonthDisplay { get; init; } = string.Empty;
    public int InvoiceCount { get; init; }
    public decimal TotalRevenue { get; init; }
    public decimal TotalPaidAmount { get; init; }
    public decimal TotalOutstandingAmount { get; init; }

    public static MonthlyRevenueViewModel FromDto(MonthlyRevenueDto dto)
    {
        var monthNames = new[] { "", "فروردین", "اردیبهشت", "خرداد", "تیر", "مرداد", "شهریور", "مهر", "آبان", "آذر", "دی", "بهمن", "اسفند" };
        
        return new MonthlyRevenueViewModel
        {
            Year = dto.Year,
            Month = dto.Month,
            MonthDisplay = dto.Month >= 1 && dto.Month <= 12 ? monthNames[dto.Month] : dto.Month.ToString(),
            InvoiceCount = dto.InvoiceCount,
            TotalRevenue = dto.TotalRevenue,
            TotalPaidAmount = dto.TotalPaidAmount,
            TotalOutstandingAmount = dto.TotalOutstandingAmount
        };
    }
}


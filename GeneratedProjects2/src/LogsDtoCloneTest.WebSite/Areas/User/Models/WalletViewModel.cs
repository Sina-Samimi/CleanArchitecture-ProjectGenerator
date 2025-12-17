using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LogsDtoCloneTest.WebSite.Areas.User.Models;

public sealed class WalletDashboardViewModel
{
    public required WalletSummaryViewModel Summary { get; init; }

    public required IReadOnlyCollection<WalletTransactionViewModel> Transactions { get; init; }

    public required IReadOnlyCollection<WalletInvoiceViewModel> Invoices { get; init; }

    public WalletCartViewModel? Cart { get; init; }

    public required ChargeWalletInputModel Charge { get; init; }
}

public sealed class WalletSummaryViewModel
{
    public required decimal Balance { get; init; }

    public required string Currency { get; init; }

    public required bool IsLocked { get; init; }

    public required DateTimeOffset LastActivityOn { get; init; }
}

public sealed class WalletTransactionViewModel
{
    public required Guid Id { get; init; }

    public required decimal Amount { get; init; }

    public required string Type { get; init; }

    public required string Status { get; init; }

    public required decimal BalanceAfter { get; init; }

    public required string Reference { get; init; }

    public string? Description { get; init; }

    public Guid? InvoiceId { get; init; }

    public DateTimeOffset OccurredAt { get; init; }
}

public sealed class WalletInvoiceViewModel
{
    public required Guid Id { get; init; }

    public required string InvoiceNumber { get; init; }

    public required string Title { get; init; }

    public required string Status { get; init; }

    public required decimal GrandTotal { get; init; }

    public required decimal OutstandingAmount { get; init; }

    public required DateTimeOffset IssueDate { get; init; }
}

public sealed class WalletCartViewModel
{
    public required Guid Id { get; init; }

    public required int ItemCount { get; init; }

    public required decimal Subtotal { get; init; }

    public required decimal DiscountTotal { get; init; }

    public required decimal GrandTotal { get; init; }

    public required DateTimeOffset UpdatedAt { get; init; }

    public required IReadOnlyCollection<WalletCartItemViewModel> Items { get; init; }
}

public sealed class WalletCartItemViewModel
{
    public required Guid ProductId { get; init; }

    public required string ProductName { get; init; }

    public required int Quantity { get; init; }

    public required decimal UnitPrice { get; init; }

    public required decimal LineTotal { get; init; }

    public string? ThumbnailPath { get; init; }

    public required string ProductType { get; init; }
}

public sealed class ChargeWalletInputModel
{
    [Required(ErrorMessage = "مبلغ شارژ کیف پول الزامی است.")]
    [Range(1000, 1_000_000_000, ErrorMessage = "مبلغ باید بین ۱٬۰۰۰ تا ۱٬۰۰۰٬۰۰۰٬۰۰۰ ریال باشد.")]
    [Display(Name = "مبلغ شارژ")]
    public decimal Amount { get; set; }

    [StringLength(200, ErrorMessage = "توضیحات نمی‌تواند بیش از ۲۰۰ کاراکتر باشد.")]
    [Display(Name = "توضیحات تراکنش")]
    public string? Description { get; set; }

    public string Currency { get; set; } = "IRT";
}

public sealed class UserInvoiceDetailViewModel
{
    public Guid Id { get; init; }

    public string InvoiceNumber { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public string? Description { get; init; }

    public string Status { get; init; } = string.Empty;

    public string Currency { get; init; } = "تومان";

    public decimal Subtotal { get; init; }

    public decimal DiscountTotal { get; init; }

    public decimal TaxAmount { get; init; }

    public decimal AdjustmentAmount { get; init; }

    public decimal GrandTotal { get; init; }

    public decimal PaidAmount { get; init; }

    public decimal OutstandingAmount { get; init; }

    public DateTimeOffset IssueDate { get; init; }

    public DateTimeOffset? DueDate { get; init; }

    public string? ExternalReference { get; init; }

    public IReadOnlyCollection<UserInvoiceItemViewModel> Items { get; init; } = Array.Empty<UserInvoiceItemViewModel>();

    public IReadOnlyCollection<UserInvoiceTransactionViewModel> Transactions { get; init; } = Array.Empty<UserInvoiceTransactionViewModel>();
}

public sealed class UserInvoiceItemViewModel
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string? Description { get; init; }

    public string ItemType { get; init; } = string.Empty;

    public Guid? ReferenceId { get; init; }

    public decimal Quantity { get; init; }

    public decimal UnitPrice { get; init; }

    public decimal? DiscountAmount { get; init; }

    public decimal Subtotal { get; init; }

    public decimal Total { get; init; }

    public IReadOnlyCollection<UserInvoiceItemAttributeViewModel> Attributes { get; init; } = Array.Empty<UserInvoiceItemAttributeViewModel>();
}

public sealed class UserInvoiceItemAttributeViewModel
{
    public string Key { get; init; } = string.Empty;

    public string Value { get; init; } = string.Empty;
}

public sealed class UserInvoiceTransactionViewModel
{
    public Guid Id { get; init; }

    public decimal Amount { get; init; }

    public string Method { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public string Reference { get; init; } = string.Empty;

    public string? GatewayName { get; init; }

    public string? Description { get; init; }

    public string? Metadata { get; init; }

    public DateTimeOffset OccurredAt { get; init; }
}

public sealed class InvoicePaymentOptionsViewModel
{
    public Guid InvoiceId { get; init; }

    public string InvoiceNumber { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public string Currency { get; init; } = "تومان";

    public decimal GrandTotal { get; init; }

    public decimal PaidAmount { get; init; }

    public decimal OutstandingAmount { get; init; }

    public DateTimeOffset IssueDate { get; init; }

    public DateTimeOffset? DueDate { get; init; }

    public decimal WalletBalance { get; init; }

    public bool IsWalletLocked { get; init; }

    public bool WalletCanCover { get; init; }

    public bool IsWalletChargeInvoice { get; init; }
}

public sealed class BankPaymentSessionViewModel
{
    public Guid InvoiceId { get; init; }

    public string InvoiceNumber { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public string GatewayName { get; init; } = string.Empty;

    public string Reference { get; init; } = string.Empty;

    public string PaymentUrl { get; init; } = string.Empty;

    public DateTimeOffset ExpiresAt { get; init; }

    public decimal Amount { get; init; }

    public string Currency { get; init; } = "تومان";

    public string? Description { get; init; }
}

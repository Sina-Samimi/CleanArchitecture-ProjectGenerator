using System;
using System.Collections.Generic;
using Attar.Domain.Enums;

namespace Attar.WebSite.Areas.User.Models;

public sealed class WalletWithdrawalRequestsViewModel
{
    public List<WalletWithdrawalRequestListItemViewModel> Requests { get; set; } = new();
    public decimal WalletBalance { get; set; }
    public string Currency { get; set; } = "IRT";
    public DateTimeOffset GeneratedAt { get; set; }
}

public sealed class WalletWithdrawalRequestListItemViewModel
{
    public Guid Id { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string? BankAccountNumber { get; set; }
    public string? CardNumber { get; set; }
    public string? Iban { get; set; }
    public string? BankName { get; set; }
    public string? AccountHolderName { get; set; }
    public string? Description { get; set; }
    public string? AdminNotes { get; set; }
    public WithdrawalRequestStatus Status { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }
    public DateTimeOffset CreateDate { get; set; }
    public DateTimeOffset UpdateDate { get; set; }
}

public sealed class CreateWalletWithdrawalRequestViewModel
{
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "IRT";
    public string? BankAccountNumber { get; set; }
    public string? CardNumber { get; set; }
    public string? Iban { get; set; }
    public string? BankName { get; set; }
    public string? AccountHolderName { get; set; }
    public string? Description { get; set; }
    public decimal WalletBalance { get; set; }
}


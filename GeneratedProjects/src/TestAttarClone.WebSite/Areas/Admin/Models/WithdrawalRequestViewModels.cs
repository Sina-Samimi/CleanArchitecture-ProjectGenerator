using System;
using System.Collections.Generic;
using TestAttarClone.Domain.Enums;

namespace TestAttarClone.WebSite.Areas.Admin.Models;

public sealed class WithdrawalRequestsViewModel
{
    public List<WithdrawalRequestListItemViewModel> Requests { get; set; } = new();
    public WithdrawalRequestStatus? FilterStatus { get; set; }
    public int TotalCount { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    public DateTimeOffset GeneratedAt { get; set; }
}

public sealed class WithdrawalRequestListItemViewModel
{
    public Guid Id { get; set; }
    public WithdrawalRequestType RequestType { get; set; }
    public string? SellerId { get; set; }
    public string? SellerName { get; set; }
    public string? SellerPhoneNumber { get; set; }
    public string? UserId { get; set; }
    public string? UserName { get; set; }
    public string? UserPhoneNumber { get; set; }
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
    public string? ProcessedByUserId { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }
    public DateTimeOffset CreateDate { get; set; }
    public DateTimeOffset UpdateDate { get; set; }
}

public sealed class WithdrawalRequestDetailsViewModel
{
    public Guid Id { get; set; }
    public WithdrawalRequestType RequestType { get; set; }
    public string? SellerId { get; set; }
    public string? SellerName { get; set; }
    public string? UserId { get; set; }
    public string? UserName { get; set; }
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
    public string? ProcessedByUserId { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }
    public Guid? WalletTransactionId { get; set; }
    public DateTimeOffset CreateDate { get; set; }
    public DateTimeOffset UpdateDate { get; set; }
}

public sealed class ProcessWithdrawalRequestViewModel
{
    public Guid RequestId { get; set; }
    public string? AdminNotes { get; set; }
}


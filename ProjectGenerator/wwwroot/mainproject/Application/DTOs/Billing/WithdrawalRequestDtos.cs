using System;
using System.Collections.Generic;
using MobiRooz.Domain.Enums;

namespace MobiRooz.Application.DTOs.Billing;

public sealed record WithdrawalRequestListItemDto(
    Guid Id,
    WithdrawalRequestType RequestType,
    string? SellerId,
    string? UserId,
    decimal Amount,
    string Currency,
    string? BankAccountNumber,
    string? CardNumber,
    string? Iban,
    string? BankName,
    string? AccountHolderName,
    string? Description,
    string? AdminNotes,
    WithdrawalRequestStatus Status,
    string? ProcessedByUserId,
    DateTimeOffset? ProcessedAt,
    Guid? WalletTransactionId,
    DateTimeOffset CreateDate,
    DateTimeOffset UpdateDate);

public sealed record WithdrawalRequestListResultDto(
    IReadOnlyCollection<WithdrawalRequestListItemDto> Items,
    int TotalCount,
    int PageNumber,
    int PageSize,
    DateTimeOffset GeneratedAt)
{
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}

public sealed record WithdrawalRequestDetailsDto(
    Guid Id,
    WithdrawalRequestType RequestType,
    string? SellerId,
    string? UserId,
    decimal Amount,
    string Currency,
    string? BankAccountNumber,
    string? CardNumber,
    string? Iban,
    string? BankName,
    string? AccountHolderName,
    string? Description,
    string? AdminNotes,
    WithdrawalRequestStatus Status,
    string? ProcessedByUserId,
    DateTimeOffset? ProcessedAt,
    Guid? WalletTransactionId,
    WalletTransactionListItemDto? WalletTransaction,
    Guid? PaymentInvoiceId,
    DateTimeOffset CreateDate,
    DateTimeOffset UpdateDate);

public static class WithdrawalRequestDtoMapper
{
    public static WithdrawalRequestListItemDto ToListItemDto(this Domain.Entities.Billing.WithdrawalRequest request)
        => new(
            request.Id,
            request.RequestType,
            request.SellerId,
            request.UserId,
            request.Amount,
            request.Currency,
            request.BankAccountNumber,
            request.CardNumber,
            request.Iban,
            request.BankName,
            request.AccountHolderName,
            request.Description,
            request.AdminNotes,
            request.Status,
            request.ProcessedByUserId,
            request.ProcessedAt,
            request.WalletTransactionId,
            request.CreateDate,
            request.UpdateDate);

    public static WithdrawalRequestDetailsDto ToDetailsDto(this Domain.Entities.Billing.WithdrawalRequest request, Guid? paymentInvoiceId = null)
        => new(
            request.Id,
            request.RequestType,
            request.SellerId,
            request.UserId,
            request.Amount,
            request.Currency,
            request.BankAccountNumber,
            request.CardNumber,
            request.Iban,
            request.BankName,
            request.AccountHolderName,
            request.Description,
            request.AdminNotes,
            request.Status,
            request.ProcessedByUserId,
            request.ProcessedAt,
            request.WalletTransactionId,
            request.WalletTransaction?.ToListItemDto(),
            paymentInvoiceId,
            request.CreateDate,
            request.UpdateDate);
}


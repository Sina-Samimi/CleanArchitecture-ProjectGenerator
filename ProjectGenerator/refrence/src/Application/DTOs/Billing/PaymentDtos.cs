using System;
using Arsis.Domain.Enums;

namespace Arsis.Application.DTOs.Billing;

public sealed record BankPaymentRequest(
    Guid InvoiceId,
    string InvoiceNumber,
    string Title,
    decimal Amount,
    string Currency,
    string UserId,
    string? Description);

public sealed record BankPaymentSessionDto(
    string GatewayName,
    string Reference,
    Uri PaymentUrl,
    DateTimeOffset ExpiresAt,
    decimal Amount,
    string Currency,
    string SessionToken,
    string? Description);

public sealed record BankPaymentReceiptDto(
    string GatewayName,
    string Reference,
    TransactionStatus Status,
    decimal Amount,
    string Currency,
    DateTimeOffset ProcessedAt,
    string? TrackingCode,
    string? Message);

public sealed record InvoicePaymentResultDto(
    Guid InvoiceId,
    string InvoiceNumber,
    PaymentMethod Method,
    WalletTransactionListItemDto? WalletTransaction,
    BankPaymentSessionDto? BankSession,
    BankPaymentReceiptDto? BankReceipt)
{
    public static InvoicePaymentResultDto FromWallet(Guid invoiceId, string invoiceNumber, WalletTransactionListItemDto walletTransaction)
        => new(
            invoiceId,
            invoiceNumber,
            PaymentMethod.Wallet,
            walletTransaction,
            null,
            null);

    public static InvoicePaymentResultDto FromBankSession(Guid invoiceId, string invoiceNumber, BankPaymentSessionDto session)
        => new(
            invoiceId,
            invoiceNumber,
            PaymentMethod.OnlineGateway,
            null,
            session,
            null);

    public static InvoicePaymentResultDto FromBankReceipt(Guid invoiceId, string invoiceNumber, BankPaymentReceiptDto receipt)
        => new(
            invoiceId,
            invoiceNumber,
            PaymentMethod.OnlineGateway,
            null,
            null,
            receipt);
}

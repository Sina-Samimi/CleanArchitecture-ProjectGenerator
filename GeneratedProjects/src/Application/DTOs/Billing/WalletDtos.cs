using System;
using System.Collections.Generic;
using System.Linq;
using TestAttarClone.Domain.Entities.Billing;
using TestAttarClone.Domain.Entities.Orders;
using TestAttarClone.Domain.Enums;

namespace TestAttarClone.Application.DTOs.Billing;

public sealed record WalletSummaryDto(
    decimal Balance,
    string Currency,
    bool IsLocked,
    DateTimeOffset LastActivityOn);

public sealed record WalletTransactionListItemDto(
    Guid Id,
    decimal Amount,
    WalletTransactionType Type,
    TransactionStatus Status,
    decimal BalanceAfter,
    string Reference,
    string? Description,
    Guid? InvoiceId,
    Guid? PaymentTransactionId,
    DateTimeOffset OccurredAt);

public sealed record WalletInvoiceSnapshotDto(
    Guid Id,
    string InvoiceNumber,
    string Title,
    InvoiceStatus Status,
    decimal GrandTotal,
    decimal OutstandingAmount,
    DateTimeOffset IssueDate);

public sealed record WalletCartItemDto(
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal,
    string? ThumbnailPath,
    ProductType ProductType);

public sealed record WalletCartDto(
    Guid Id,
    int ItemCount,
    decimal Subtotal,
    decimal DiscountTotal,
    decimal GrandTotal,
    DateTimeOffset UpdatedAt,
    IReadOnlyCollection<WalletCartItemDto> Items);

public sealed record WalletDashboardDto(
    WalletSummaryDto Summary,
    IReadOnlyCollection<WalletTransactionListItemDto> Transactions,
    IReadOnlyCollection<WalletInvoiceSnapshotDto> Invoices,
    WalletCartDto? Cart,
    DateTimeOffset GeneratedAt);

public static class WalletDtoMapper
{
    public static WalletSummaryDto ToSummaryDto(this WalletAccount account)
        => new(
            account.Balance,
            account.Currency,
            account.IsLocked,
            account.LastActivityOn);

    public static WalletTransactionListItemDto ToListItemDto(this WalletTransaction transaction)
        => new(
            transaction.Id,
            transaction.Amount,
            transaction.Type,
            transaction.Status,
            transaction.BalanceAfterTransaction,
            transaction.Reference,
            transaction.Description,
            transaction.InvoiceId,
            transaction.PaymentTransactionId,
            transaction.OccurredAt);

    public static WalletInvoiceSnapshotDto ToWalletSnapshotDto(this Invoice invoice)
        => new(
            invoice.Id,
            invoice.InvoiceNumber,
            invoice.Title,
            invoice.Status,
            invoice.GrandTotal,
            invoice.OutstandingAmount,
            invoice.IssueDate);

    public static WalletCartDto ToWalletDto(this ShoppingCart cart)
    {
        var items = cart.Items
            .Select(item => new WalletCartItemDto(
                item.ProductId,
                item.ProductName,
                item.Quantity,
                item.UnitPrice,
                item.LineTotal,
                item.ThumbnailPath,
                item.ProductType))
            .ToArray();

        return new WalletCartDto(
            cart.Id,
            items.Sum(item => item.Quantity),
            cart.Subtotal,
            cart.DiscountTotal,
            cart.GrandTotal,
            cart.UpdateDate,
            items);
    }
}

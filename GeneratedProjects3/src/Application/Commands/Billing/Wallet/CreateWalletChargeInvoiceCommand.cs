using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LogTableRenameTest.Application.Abstractions.Messaging;
using LogTableRenameTest.Application.Commands.Billing;
using LogTableRenameTest.Application.Interfaces;
using LogTableRenameTest.Domain.Enums;
using LogTableRenameTest.SharedKernel.BaseTypes;
using LogTableRenameTest.SharedKernel.Helpers;
using MediatR;

namespace LogTableRenameTest.Application.Commands.Billing.Wallet;

public sealed record CreateWalletChargeInvoiceCommand(
    string UserId,
    decimal Amount,
    string? Currency,
    string? Description) : ICommand<Guid>;

public sealed class CreateWalletChargeInvoiceCommandHandler : ICommandHandler<CreateWalletChargeInvoiceCommand, Guid>
{
    private const string DefaultCurrency = "IRT";

    private readonly ISender _sender;

    public CreateWalletChargeInvoiceCommandHandler(ISender sender)
    {
        _sender = sender;
    }

    public async Task<Result<Guid>> Handle(CreateWalletChargeInvoiceCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.UserId))
        {
            return Result<Guid>.Failure("شناسه کاربر معتبر نیست.");
        }

        if (request.Amount <= 0)
        {
            return Result<Guid>.Failure("مبلغ شارژ کیف پول باید بیشتر از صفر باشد.");
        }

        var currency = string.IsNullOrWhiteSpace(request.Currency)
            ? DefaultCurrency
            : request.Currency!.Trim().ToUpperInvariant();

        var externalReference = ReferenceGenerator.GenerateReadableReference("WCH", DateTimeOffset.UtcNow);

        var invoiceItems = new List<CreateInvoiceCommand.Item>
        {
            new(
                Name: "شارژ کیف پول",
                Description: request.Description,
                ItemType: InvoiceItemType.Service,
                ReferenceId: null,
                Quantity: 1,
                UnitPrice: request.Amount,
                DiscountAmount: null,
                Attributes: new List<CreateInvoiceCommand.Attribute>
                {
                    new("WalletCharge", "true"),
                    new("Amount", request.Amount.ToString("F2"))
                })
        };

        var createInvoiceCommand = new CreateInvoiceCommand(
            InvoiceNumber: null,
            Title: "شارژ کیف پول",
            Description: string.IsNullOrWhiteSpace(request.Description)
                ? $"شارژ کیف پول به مبلغ {request.Amount:N0} {currency}"
                : request.Description,
            Currency: currency,
            UserId: request.UserId.Trim(),
            IssueDate: DateTimeOffset.Now,
            DueDate: DateTimeOffset.Now.AddDays(7),
            TaxAmount: 0,
            AdjustmentAmount: 0,
            ExternalReference: externalReference,
            Items: invoiceItems);

        var result = await _sender.Send(createInvoiceCommand, cancellationToken);

        return result;
    }
}


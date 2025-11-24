using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arsis.Application.Abstractions.Messaging;
using Arsis.Application.Interfaces;
using Arsis.Domain.Entities.Billing;
using Arsis.Domain.Enums;
using Arsis.Domain.Exceptions;
using Arsis.SharedKernel.BaseTypes;

namespace Arsis.Application.Commands.Billing;

public sealed record CreateInvoiceCommand(
    string? InvoiceNumber,
    string Title,
    string? Description,
    string Currency,
    string? UserId,
    DateTimeOffset IssueDate,
    DateTimeOffset? DueDate,
    decimal TaxAmount,
    decimal AdjustmentAmount,
    string? ExternalReference,
    IReadOnlyCollection<CreateInvoiceCommand.Item> Items) : ICommand<Guid>
{
    public sealed record Item(
        string Name,
        string? Description,
        InvoiceItemType ItemType,
        Guid? ReferenceId,
        decimal Quantity,
        decimal UnitPrice,
        decimal? DiscountAmount,
        IReadOnlyCollection<Attribute>? Attributes);

    public sealed record Attribute(string Key, string Value);

    public sealed class Handler : ICommandHandler<CreateInvoiceCommand, Guid>
    {
        private readonly IInvoiceRepository _invoiceRepository;
        private readonly IAuditContext _auditContext;

        public Handler(IInvoiceRepository invoiceRepository, IAuditContext auditContext)
        {
            _invoiceRepository = invoiceRepository;
            _auditContext = auditContext;
        }

        public async Task<Result<Guid>> Handle(CreateInvoiceCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Title))
            {
                return Result<Guid>.Failure("عنوان فاکتور الزامی است.");
            }

            if (string.IsNullOrWhiteSpace(request.Currency))
            {
                return Result<Guid>.Failure("واحد پول فاکتور مشخص نشده است.");
            }

            if (request.Items is null || request.Items.Count == 0)
            {
                return Result<Guid>.Failure("حداقل یک آیتم برای فاکتور لازم است.");
            }

            var invoiceNumber = string.IsNullOrWhiteSpace(request.InvoiceNumber)
                ? Invoice.GenerateInvoiceNumber()
                : request.InvoiceNumber.Trim();

            var exists = await _invoiceRepository.ExistsByNumberAsync(invoiceNumber, null, cancellationToken);
            if (exists)
            {
                return Result<Guid>.Failure("شماره فاکتور تکراری است.");
            }

            try
            {
                var invoice = new Invoice(
                    invoiceNumber,
                    request.Title,
                    request.Description,
                    request.Currency,
                    request.UserId,
                    request.IssueDate,
                    request.DueDate,
                    request.TaxAmount,
                    request.AdjustmentAmount,
                    request.ExternalReference);

                foreach (var item in request.Items)
                {
                    var attributes = item.Attributes?.Select(attribute => (attribute.Key, attribute.Value)).ToArray();
                    invoice.AddItem(
                        item.Name,
                        item.Description,
                        item.ItemType,
                        item.ReferenceId,
                        item.Quantity,
                        item.UnitPrice,
                        item.DiscountAmount,
                        attributes);
                }

                // Note: All audit fields (CreatorId, CreateDate, UpdateDate, Ip) will be set automatically by AuditInterceptor

                await _invoiceRepository.AddAsync(invoice, cancellationToken);

                return Result<Guid>.Success(invoice.Id);
            }
            catch (DomainException ex)
            {
                return Result<Guid>.Failure(ex.Message);
            }
        }
    }
}

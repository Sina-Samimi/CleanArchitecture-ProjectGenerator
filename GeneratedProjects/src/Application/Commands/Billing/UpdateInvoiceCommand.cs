using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TestAttarClone.Application.Abstractions.Messaging;
using TestAttarClone.Application.Interfaces;
using TestAttarClone.Domain.Entities.Billing;
using TestAttarClone.Domain.Enums;
using TestAttarClone.Domain.Exceptions;
using TestAttarClone.SharedKernel.BaseTypes;
using Microsoft.Extensions.Logging;

namespace TestAttarClone.Application.Commands.Billing;

public sealed record UpdateInvoiceCommand(
    Guid Id,
    string InvoiceNumber,
    string Title,
    string? Description,
    string Currency,
    string? UserId,
    DateTimeOffset IssueDate,
    DateTimeOffset? DueDate,
    decimal TaxAmount,
    decimal AdjustmentAmount,
    string? ExternalReference,
    IReadOnlyCollection<UpdateInvoiceCommand.Item> Items) : ICommand<Guid>
{
    public sealed record Item(
        Guid? Id,
        string Name,
        string? Description,
        InvoiceItemType ItemType,
        Guid? ReferenceId,
        decimal Quantity,
        decimal UnitPrice,
        decimal? DiscountAmount,
        IReadOnlyCollection<Attribute>? Attributes);

    public sealed record Attribute(Guid? Id, string Key, string Value);

    public sealed class Handler : ICommandHandler<UpdateInvoiceCommand, Guid>
    {
        private readonly IInvoiceRepository _invoiceRepository;
        private readonly IAuditContext _auditContext;
        private readonly ILogger<Handler> _logger;

        public Handler(IInvoiceRepository invoiceRepository, IAuditContext auditContext, ILogger<Handler> logger)
        {
            _invoiceRepository = invoiceRepository;
            _auditContext = auditContext;
            _logger = logger;
        }

        public async Task<Result<Guid>> Handle(UpdateInvoiceCommand request, CancellationToken cancellationToken)
        {
            if (request.Id == Guid.Empty)
            {
                return Result<Guid>.Failure("شناسه فاکتور معتبر نیست.");
            }

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

            var normalizedInvoiceNumberInput = string.IsNullOrWhiteSpace(request.InvoiceNumber)
                ? null
                : request.InvoiceNumber.Trim();

            var result = await _invoiceRepository.MutateAsync(
                request.Id,
                includeDetails: true,
                async (invoice, ct) =>
                {
                    var invoiceNumber = normalizedInvoiceNumberInput ?? invoice.InvoiceNumber;

                    if (!invoice.InvoiceNumber.Equals(invoiceNumber, StringComparison.OrdinalIgnoreCase))
                    {
                        var exists = await _invoiceRepository.ExistsByNumberAsync(invoiceNumber, request.Id, ct);
                        if (exists)
                        {
                            return Result<Guid>.Failure("شماره فاکتور تکراری است.");
                        }
                    }

                    try
                    {
                        invoice.SetInvoiceNumber(invoiceNumber);
                        invoice.SetTitle(request.Title);
                        invoice.SetDescription(request.Description);
                        invoice.SetCurrency(request.Currency);
                        invoice.SetUser(request.UserId);
                        invoice.SetExternalReference(request.ExternalReference);
                        invoice.SetIssueDate(request.IssueDate);
                        invoice.SetDueDate(request.DueDate);
                        invoice.SetTaxAmount(request.TaxAmount);
                        invoice.SetAdjustmentAmount(request.AdjustmentAmount);

                        ApplyItemUpdates(invoice, request.Items);

                        // Note: UpdateDate, UpdaterId, and Ip will be set automatically by SaveChangesInterceptor
                        // Don't set them manually here to avoid concurrency conflicts

                        return Result<Guid>.Success(invoice.Id);
                    }
                    catch (DomainException ex)
                    {
                        return Result<Guid>.Failure(ex.Message);
                    }
                },
                cancellationToken,
                notFoundMessage: "فاکتور مورد نظر یافت نشد.");

            if (!result.IsSuccess)
            {
                _logger.LogError(
                    "UpdateInvoiceCommand failed for InvoiceId={InvoiceId}. Error={Error}",
                    request.Id,
                    result.Error ?? "Unknown error");
            }

            return result;
        }

        private static void ApplyItemUpdates(Invoice invoice, IReadOnlyCollection<Item> requestedItems)
        {
            ArgumentNullException.ThrowIfNull(invoice);
            ArgumentNullException.ThrowIfNull(requestedItems);

            var existingItems = invoice.Items.ToDictionary(item => item.Id);
            var processedItemIds = new HashSet<Guid>();

            foreach (var item in requestedItems)
            {
                var attributes = item.Attributes?
                    .Where(attribute => !string.IsNullOrWhiteSpace(attribute.Key))
                    .Select(attribute => (attribute.Key, attribute.Value))
                    .ToArray();

                if (item.Id.HasValue && existingItems.TryGetValue(item.Id.Value, out var existingItem))
                {
                    existingItem.UpdateSnapshot(
                        item.Name,
                        item.Description,
                        item.ItemType,
                        item.ReferenceId,
                        item.Quantity,
                        item.UnitPrice,
                        item.DiscountAmount);

                    SyncAttributes(existingItem, item.Attributes);
                    processedItemIds.Add(existingItem.Id);
                    continue;
                }

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

            foreach (var existingItem in existingItems.Values)
            {
                if (!processedItemIds.Contains(existingItem.Id))
                {
                    invoice.RemoveItem(existingItem.Id);
                }
            }
        }

        private static void SyncAttributes(
            TestAttarClone.Domain.Entities.Billing.InvoiceItem item,
            IReadOnlyCollection<Attribute>? requestedAttributes)
        {
            requestedAttributes ??= Array.Empty<Attribute>();

            var existingAttributes = item.Attributes.ToDictionary(attribute => attribute.Id);
            var processedAttributeIds = new HashSet<Guid>();

            foreach (var attribute in requestedAttributes.Where(a => !string.IsNullOrWhiteSpace(a.Key)))
            {
                if (attribute.Id.HasValue && existingAttributes.TryGetValue(attribute.Id.Value, out var existingAttribute))
                {
                    existingAttribute.SetKey(attribute.Key);
                    existingAttribute.SetValue(attribute.Value);
                    processedAttributeIds.Add(existingAttribute.Id);
                    continue;
                }

                var addedAttribute = item.AddAttribute(attribute.Key, attribute.Value);
                processedAttributeIds.Add(addedAttribute.Id);
            }

            foreach (var existingAttribute in existingAttributes.Values)
            {
                if (!processedAttributeIds.Contains(existingAttribute.Id))
                {
                    item.RemoveAttribute(existingAttribute.Id);
                }
            }
        }
    }
}

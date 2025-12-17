using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Attar.Application.Abstractions.Messaging;
using Attar.Application.Interfaces;
using Attar.Application.Queries.Billing;
using Attar.Domain.Entities.Billing;
using Attar.Domain.Entities.Orders;
using Attar.Domain.Enums;
using Attar.SharedKernel.BaseTypes;
using MediatR;

namespace Attar.Application.Commands.Orders;

public sealed record CreateShipmentTrackingCommand(
    Guid InvoiceItemId,
    ShipmentStatus Status,
    DateTimeOffset StatusDate,
    string? TrackingNumber = null,
    string? Notes = null) : ICommand<Guid>
{
    public sealed class Handler : ICommandHandler<CreateShipmentTrackingCommand, Guid>
    {
        private readonly IShipmentTrackingRepository _trackingRepository;
        private readonly IInvoiceRepository _invoiceRepository;
        private readonly IAuditContext _auditContext;
        private readonly IMediator _mediator;

        public Handler(
            IShipmentTrackingRepository trackingRepository,
            IInvoiceRepository invoiceRepository,
            IAuditContext auditContext,
            IMediator mediator)
        {
            _trackingRepository = trackingRepository;
            _invoiceRepository = invoiceRepository;
            _auditContext = auditContext;
            _mediator = mediator;
        }

        public async Task<Result<Guid>> Handle(CreateShipmentTrackingCommand request, CancellationToken cancellationToken)
        {
            // Find invoice item by querying invoices
            // We'll search through invoices to find the one containing this item
            var invoicesResult = await _mediator.Send(
                new GetInvoiceListQuery(new Application.DTOs.Billing.InvoiceListFilterDto(null, null, null, null, null, 1, 1000)),
                cancellationToken);

            if (!invoicesResult.IsSuccess)
            {
                return Result<Guid>.Failure("خطا در دریافت لیست فاکتورها.");
            }

            InvoiceItem? invoiceItem = null;

            foreach (var inv in invoicesResult.Value.Items)
            {
                var detail = await _invoiceRepository.GetByIdAsync(inv.Id, cancellationToken, includeDetails: true);
                if (detail is null) continue;

                var item = detail.Items.FirstOrDefault(i => i.Id == request.InvoiceItemId);
                if (item is not null)
                {
                    invoiceItem = item;
                    break;
                }
            }

            if (invoiceItem is null)
            {
                return Result<Guid>.Failure("آیتم فاکتور مورد نظر یافت نشد.");
            }

            // Check if item is a physical product
            if (invoiceItem.ItemType != InvoiceItemType.Product || !invoiceItem.ReferenceId.HasValue)
            {
                return Result<Guid>.Failure("فقط برای محصولات فیزیکی می‌توان tracking ایجاد کرد.");
            }

            // Check if tracking already exists
            var existing = await _trackingRepository.GetByInvoiceItemIdAsync(request.InvoiceItemId, cancellationToken);
            if (existing is not null)
            {
                return Result<Guid>.Failure("برای این آیتم قبلاً tracking ایجاد شده است. از به‌روزرسانی استفاده کنید.");
            }

            var audit = _auditContext.Capture();

            var tracking = new ShipmentTracking(
                request.InvoiceItemId,
                request.Status,
                request.StatusDate,
                request.TrackingNumber,
                request.Notes);

            tracking.CreatorId = audit.UserId;
            tracking.CreateDate = audit.Timestamp;
            tracking.UpdateDate = audit.Timestamp;
            tracking.SetUpdatedBy(audit.UserId);
            tracking.Ip = audit.IpAddress;

            await _trackingRepository.AddAsync(tracking, cancellationToken);

            return Result<Guid>.Success(tracking.Id);
        }
    }
}


using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LogsDtoCloneTest.Application.Abstractions.Messaging;
using LogsDtoCloneTest.Application.DTOs.Orders;
using LogsDtoCloneTest.Application.Interfaces;
using LogsDtoCloneTest.SharedKernel.BaseTypes;

namespace LogsDtoCloneTest.Application.Queries.Orders;

public sealed record GetShipmentTrackingsByInvoiceQuery(Guid InvoiceId) : IQuery<ShipmentTrackingListDto>
{
    public sealed class Handler : IQueryHandler<GetShipmentTrackingsByInvoiceQuery, ShipmentTrackingListDto>
    {
        private readonly IShipmentTrackingRepository _trackingRepository;
        private readonly IInvoiceRepository _invoiceRepository;
        private readonly IProductRepository _productRepository;

        public Handler(
            IShipmentTrackingRepository trackingRepository,
            IInvoiceRepository invoiceRepository,
            IProductRepository productRepository)
        {
            _trackingRepository = trackingRepository;
            _invoiceRepository = invoiceRepository;
            _productRepository = productRepository;
        }

        public async Task<Result<ShipmentTrackingListDto>> Handle(GetShipmentTrackingsByInvoiceQuery request, CancellationToken cancellationToken)
        {
            var invoice = await _invoiceRepository.GetByIdAsync(request.InvoiceId, cancellationToken, includeDetails: true);
            if (invoice is null)
            {
                var emptyDto = new ShipmentTrackingListDto(
                    request.InvoiceId,
                    string.Empty,
                    Array.Empty<ShipmentTrackingDto>());
                return Result<ShipmentTrackingListDto>.Success(emptyDto);
            }

            var trackings = await _trackingRepository.GetByInvoiceIdAsync(request.InvoiceId, cancellationToken);
            var trackingDtos = new List<ShipmentTrackingDto>();

            foreach (var tracking in trackings)
            {
                var productName = string.Empty;
                if (tracking.InvoiceItem.ReferenceId.HasValue)
                {
                    var product = await _productRepository.GetByIdAsync(tracking.InvoiceItem.ReferenceId.Value, cancellationToken);
                    productName = product?.Name ?? string.Empty;
                }

                trackingDtos.Add(new ShipmentTrackingDto(
                    tracking.Id,
                    tracking.InvoiceItemId,
                    tracking.InvoiceItem.Name,
                    tracking.InvoiceItem.ReferenceId,
                    productName,
                    tracking.Status,
                    tracking.TrackingNumber,
                    tracking.Notes,
                    tracking.StatusDate,
                    tracking.UpdatedById,
                    tracking.UpdatedBy?.UserName ?? string.Empty));
            }

            var dto = new ShipmentTrackingListDto(
                invoice.Id,
                invoice.InvoiceNumber,
                trackingDtos);
            
            return Result<ShipmentTrackingListDto>.Success(dto);
        }
    }
}


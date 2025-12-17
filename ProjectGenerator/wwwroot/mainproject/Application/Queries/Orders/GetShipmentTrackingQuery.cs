using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Attar.Application.Abstractions.Messaging;
using Attar.Application.DTOs.Orders;
using Attar.Application.Interfaces;
using Attar.Domain.Enums;
using Attar.SharedKernel.BaseTypes;

namespace Attar.Application.Queries.Orders;

public sealed record GetShipmentTrackingQuery(Guid InvoiceItemId) : IQuery<ShipmentTrackingDetailDto?>
{
    public sealed class Handler : IQueryHandler<GetShipmentTrackingQuery, ShipmentTrackingDetailDto?>
    {
        private readonly IShipmentTrackingRepository _trackingRepository;
        private readonly IProductRepository _productRepository;

        public Handler(
            IShipmentTrackingRepository trackingRepository,
            IProductRepository productRepository)
        {
            _trackingRepository = trackingRepository;
            _productRepository = productRepository;
        }

        public async Task<Result<ShipmentTrackingDetailDto?>> Handle(GetShipmentTrackingQuery request, CancellationToken cancellationToken)
        {
            var tracking = await _trackingRepository.GetByInvoiceItemIdAsync(request.InvoiceItemId, cancellationToken);
            if (tracking is null)
            {
                return Result<ShipmentTrackingDetailDto?>.Success(null);
            }

            var invoice = tracking.InvoiceItem.Invoice;
            var productName = string.Empty;

            if (tracking.InvoiceItem.ReferenceId.HasValue)
            {
                var product = await _productRepository.GetByIdAsync(tracking.InvoiceItem.ReferenceId.Value, cancellationToken);
                productName = product?.Name ?? string.Empty;
            }

            var dto = new ShipmentTrackingDetailDto(
                tracking.Id,
                tracking.InvoiceItemId,
                invoice.Id,
                invoice.InvoiceNumber,
                tracking.InvoiceItem.Name,
                tracking.InvoiceItem.ReferenceId,
                productName,
                tracking.Status,
                tracking.TrackingNumber,
                tracking.Notes,
                tracking.StatusDate,
                tracking.UpdatedById,
                tracking.UpdatedBy?.UserName ?? string.Empty,
                Array.Empty<ShipmentStatusHistoryDto>()); // History can be added later if needed

            return Result<ShipmentTrackingDetailDto?>.Success(dto);
        }
    }
}


using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MobiRooz.Application.Abstractions.Messaging;
using MobiRooz.Application.DTOs.Orders;
using MobiRooz.Application.Interfaces;
using MobiRooz.SharedKernel.BaseTypes;

namespace MobiRooz.Application.Queries.Orders;

public sealed record GetUserShipmentTrackingsQuery(string UserId) : IQuery<IReadOnlyCollection<ShipmentTrackingDto>>
{
    public sealed class Handler : IQueryHandler<GetUserShipmentTrackingsQuery, IReadOnlyCollection<ShipmentTrackingDto>>
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

        public async Task<Result<IReadOnlyCollection<ShipmentTrackingDto>>> Handle(GetUserShipmentTrackingsQuery request, CancellationToken cancellationToken)
        {
            var trackings = await _trackingRepository.GetByUserIdAsync(request.UserId, cancellationToken);
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

            return Result<IReadOnlyCollection<ShipmentTrackingDto>>.Success(trackingDtos);
        }
    }
}


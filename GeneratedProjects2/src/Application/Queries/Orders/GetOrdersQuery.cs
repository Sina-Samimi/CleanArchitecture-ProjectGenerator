using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LogsDtoCloneTest.Application.Abstractions.Messaging;
using LogsDtoCloneTest.Application.DTOs.Orders;
using LogsDtoCloneTest.Application.Interfaces;
using LogsDtoCloneTest.Domain.Enums;
using LogsDtoCloneTest.SharedKernel.BaseTypes;

namespace LogsDtoCloneTest.Application.Queries.Orders;

public sealed record GetOrdersQuery(
    string? SellerId = null,
    string? UserId = null,
    InvoiceStatus? Status = null,
    DateTimeOffset? FromDate = null,
    DateTimeOffset? ToDate = null,
    int? PageNumber = null,
    int? PageSize = null) : IQuery<OrderListResultDto>
{
    public sealed class Handler : IQueryHandler<GetOrdersQuery, OrderListResultDto>
    {
        private readonly IInvoiceRepository _invoiceRepository;
        private readonly IProductRepository _productRepository;

        public Handler(
            IInvoiceRepository invoiceRepository,
            IProductRepository productRepository)
        {
            _invoiceRepository = invoiceRepository;
            _productRepository = productRepository;
        }

        public async Task<Result<OrderListResultDto>> Handle(GetOrdersQuery request, CancellationToken cancellationToken)
        {
            var filter = new Application.DTOs.Billing.InvoiceListFilterDto(
                SearchTerm: null,
                UserId: request.UserId,
                Status: request.Status,
                IssueDateFrom: request.FromDate,
                IssueDateTo: request.ToDate,
                PageNumber: request.PageNumber ?? 1,
                PageSize: request.PageSize ?? 20);

            var invoices = await _invoiceRepository.GetListAsync(filter, cancellationToken);

            // فیلتر کردن فقط سفارشاتی که شامل محصول فیزیکی هستند
            var ordersWithPhysicalProducts = invoices
                .Where(invoice => invoice.Items.Any(item => 
                    item.ItemType == InvoiceItemType.Product && 
                    item.ReferenceId.HasValue))
                .ToList();

            // اگر SellerId مشخص شده، فقط سفارشات محصولات آن فروشنده
            if (!string.IsNullOrWhiteSpace(request.SellerId))
            {
                var sellerProductIds = await _productRepository.GetBySellerAsync(request.SellerId, cancellationToken);
                var sellerProductIdSet = sellerProductIds.Select(p => p.Id).ToHashSet();

                ordersWithPhysicalProducts = ordersWithPhysicalProducts
                    .Where(invoice => invoice.Items.Any(item =>
                        item.ItemType == InvoiceItemType.Product &&
                        item.ReferenceId.HasValue &&
                        sellerProductIdSet.Contains(item.ReferenceId.Value)))
                    .ToList();
            }

            var orderDtos = new List<OrderDto>();

            foreach (var invoice in ordersWithPhysicalProducts)
            {
                var physicalItems = invoice.Items
                    .Where(item => item.ItemType == InvoiceItemType.Product && item.ReferenceId.HasValue)
                    .ToList();

                foreach (var item in physicalItems)
                {
                    var product = item.ReferenceId.HasValue
                        ? await _productRepository.GetByIdAsync(item.ReferenceId.Value, cancellationToken)
                        : null;

                    orderDtos.Add(new OrderDto(
                        invoice.Id,
                        invoice.InvoiceNumber,
                        invoice.IssueDate,
                        invoice.Status,
                        invoice.UserId,
                        item.Id,
                        item.Name,
                        item.Quantity,
                        item.UnitPrice,
                        item.Total,
                        item.ReferenceId,
                        item.VariantId,
                        product?.SellerId,
                        product?.Name ?? item.Name));
                }
            }

            // فیلتر کردن فاکتورهای در انتظار پرداخت برای فروشندگان
            if (!string.IsNullOrWhiteSpace(request.SellerId))
            {
                orderDtos = orderDtos
                    .Where(order => order.Status != InvoiceStatus.Pending)
                    .ToList();
            }
            
            var totalCount = orderDtos.Count;
            var pageNumber = request.PageNumber ?? 1;
            var pageSize = request.PageSize ?? 20;
            var pagedOrders = orderDtos
                .OrderByDescending(o => o.OrderDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var result = new OrderListResultDto(
                pagedOrders,
                totalCount,
                pageNumber,
                pageSize,
                DateTimeOffset.UtcNow);

            return Result<OrderListResultDto>.Success(result);
        }
    }
}


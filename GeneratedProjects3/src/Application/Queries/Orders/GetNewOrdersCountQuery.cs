using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LogTableRenameTest.Application.Abstractions.Messaging;
using LogTableRenameTest.Application.Interfaces;
using LogTableRenameTest.Domain.Enums;
using LogTableRenameTest.SharedKernel.BaseTypes;

namespace LogTableRenameTest.Application.Queries.Orders;

public sealed record GetNewOrdersCountQuery(string? SellerId = null) : IQuery<int>
{
    public sealed class Handler : IQueryHandler<GetNewOrdersCountQuery, int>
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

        public async Task<Result<int>> Handle(GetNewOrdersCountQuery request, CancellationToken cancellationToken)
        {
            // سفارشات جدید: سفارشاتی که در 24 ساعت گذشته ایجاد شده‌اند، پرداخت شده‌اند و شامل محصول فیزیکی هستند
            var sinceDate = DateTimeOffset.UtcNow.AddHours(-24);

            var filter = new Application.DTOs.Billing.InvoiceListFilterDto(
                SearchTerm: null,
                UserId: null,
                Status: InvoiceStatus.Paid, // فقط سفارشات پرداخت شده
                IssueDateFrom: sinceDate,
                IssueDateTo: null,
                PageNumber: 1,
                PageSize: 1000);

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

            return Result<int>.Success(ordersWithPhysicalProducts.Count);
        }
    }
}


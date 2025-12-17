using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Attar.Application.Abstractions.Messaging;
using Attar.Application.DTOs.Catalog;
using Attar.Application.Interfaces;
using Attar.SharedKernel.BaseTypes;

namespace Attar.Application.Queries.Catalog;

public sealed record GetProductSalesSummaryQuery(Guid ProductId) : IQuery<ProductSalesSummaryDto>
{
    public sealed class Handler : IQueryHandler<GetProductSalesSummaryQuery, ProductSalesSummaryDto>
    {
        private readonly IInvoiceRepository _invoiceRepository;

        public Handler(IInvoiceRepository invoiceRepository)
        {
            _invoiceRepository = invoiceRepository;
        }

        public async Task<Result<ProductSalesSummaryDto>> Handle(
            GetProductSalesSummaryQuery request,
            CancellationToken cancellationToken)
        {
            if (request.ProductId == Guid.Empty)
            {
                return Result<ProductSalesSummaryDto>.Failure("شناسه محصول نامعتبر است.");
            }

            var items = await _invoiceRepository.GetProductInvoiceItemsAsync(request.ProductId, cancellationToken);

            if (items.Count == 0)
            {
                var empty = new ProductSalesSummaryDto(
                    request.ProductId,
                    0,
                    0m,
                    0m,
                    0m,
                    0m,
                    null,
                    null,
                    Array.Empty<ProductSalesTrendPointDto>());

                return Result<ProductSalesSummaryDto>.Success(empty);
            }

            var validItems = items
                .Where(item => item.Invoice is not null && !item.Invoice.IsDeleted)
                .ToList();

            if (validItems.Count == 0)
            {
                var empty = new ProductSalesSummaryDto(
                    request.ProductId,
                    0,
                    0m,
                    0m,
                    0m,
                    0m,
                    null,
                    null,
                    Array.Empty<ProductSalesTrendPointDto>());

                return Result<ProductSalesSummaryDto>.Success(empty);
            }

            var totalQuantity = validItems.Sum(item => item.Quantity);
            var totalRevenue = validItems.Sum(item => item.Total);
            var totalDiscount = validItems.Sum(item => item.DiscountAmount ?? 0m);
            var orderCount = validItems
                .Select(item => item.InvoiceId)
                .Distinct()
                .Count();

            var firstSaleAt = validItems.Min(item => item.Invoice.IssueDate);
            var lastSaleAt = validItems.Max(item => item.Invoice.IssueDate);
            var averageOrderValue = orderCount > 0
                ? decimal.Round(totalRevenue / orderCount, 2, MidpointRounding.AwayFromZero)
                : 0m;

            var trend = validItems
                .GroupBy(item => new { item.Invoice.IssueDate.Year, item.Invoice.IssueDate.Month })
                .OrderBy(group => group.Key.Year)
                .ThenBy(group => group.Key.Month)
                .Select(group =>
                {
                    var sampleDate = group.First().Invoice.IssueDate;
                    var period = new DateTimeOffset(group.Key.Year, group.Key.Month, 1, 0, 0, 0, sampleDate.Offset);
                    var quantity = decimal.Round(group.Sum(item => item.Quantity), 2, MidpointRounding.AwayFromZero);
                    var revenue = decimal.Round(group.Sum(item => item.Total), 2, MidpointRounding.AwayFromZero);
                    return new ProductSalesTrendPointDto(period, quantity, revenue);
                })
                .ToArray();

            var dto = new ProductSalesSummaryDto(
                request.ProductId,
                orderCount,
                decimal.Round(totalQuantity, 2, MidpointRounding.AwayFromZero),
                decimal.Round(totalRevenue, 2, MidpointRounding.AwayFromZero),
                decimal.Round(totalDiscount, 2, MidpointRounding.AwayFromZero),
                averageOrderValue,
                firstSaleAt,
                lastSaleAt,
                trend);

            return Result<ProductSalesSummaryDto>.Success(dto);
        }
    }
}

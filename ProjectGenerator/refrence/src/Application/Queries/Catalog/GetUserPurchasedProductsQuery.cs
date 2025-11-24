using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arsis.Application.Abstractions.Messaging;
using Arsis.Application.DTOs.Catalog;
using Arsis.Application.Interfaces;
using Arsis.Domain.Enums;
using Arsis.SharedKernel.BaseTypes;

namespace Arsis.Application.Queries.Catalog;

public sealed record GetUserPurchasedProductsQuery(string UserId)
    : IQuery<IReadOnlyCollection<UserPurchasedProductDto>>;

public sealed class GetUserPurchasedProductsQueryHandler
    : IQueryHandler<GetUserPurchasedProductsQuery, IReadOnlyCollection<UserPurchasedProductDto>>
{
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IProductRepository _productRepository;

    public GetUserPurchasedProductsQueryHandler(
        IInvoiceRepository invoiceRepository,
        IProductRepository productRepository)
    {
        _invoiceRepository = invoiceRepository;
        _productRepository = productRepository;
    }

    public async Task<Result<IReadOnlyCollection<UserPurchasedProductDto>>> Handle(
        GetUserPurchasedProductsQuery request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.UserId))
        {
            return Result<IReadOnlyCollection<UserPurchasedProductDto>>.Failure("شناسه کاربر معتبر نیست.");
        }

        var invoices = await _invoiceRepository.GetListByUserAsync(request.UserId.Trim(), null, cancellationToken);
        if (invoices.Count == 0)
        {
            return Result<IReadOnlyCollection<UserPurchasedProductDto>>.Success(Array.Empty<UserPurchasedProductDto>());
        }

        var eligibleInvoices = invoices
            .Where(invoice => invoice.Status != InvoiceStatus.Draft && invoice.Status != InvoiceStatus.Cancelled)
            .ToArray();

        if (eligibleInvoices.Length == 0)
        {
            return Result<IReadOnlyCollection<UserPurchasedProductDto>>.Success(Array.Empty<UserPurchasedProductDto>());
        }

        var productItems = eligibleInvoices
            .SelectMany(invoice => invoice.Items.Select(item => (invoice, item)))
            .Where(tuple => tuple.item.ItemType == InvoiceItemType.Product)
            .ToArray();

        if (productItems.Length == 0)
        {
            return Result<IReadOnlyCollection<UserPurchasedProductDto>>.Success(Array.Empty<UserPurchasedProductDto>());
        }

        var productIds = productItems
            .Select(tuple => tuple.item.ReferenceId)
            .Where(referenceId => referenceId.HasValue && referenceId.Value != Guid.Empty)
            .Select(referenceId => referenceId!.Value)
            .Distinct()
            .ToArray();

        var products = productIds.Length == 0
            ? Array.Empty<Domain.Entities.Catalog.Product>()
            : await _productRepository.GetByIdsAsync(productIds, cancellationToken);

        var productMap = products.ToDictionary(product => product.Id);

        var result = new List<UserPurchasedProductDto>(productItems.Length);
        foreach (var (invoice, item) in productItems)
        {
            productMap.TryGetValue(item.ReferenceId ?? Guid.Empty, out var product);

            var paidTransaction = invoice.Transactions
                .Where(transaction => transaction.Status == TransactionStatus.Succeeded)
                .OrderByDescending(transaction => transaction.OccurredAt)
                .FirstOrDefault();

            var purchasedAt = paidTransaction?.OccurredAt ?? invoice.IssueDate;
            var name = product?.Name ?? item.Name;
            var summary = product?.Summary ?? item.Description;
            var categoryName = product?.Category?.Name;
            var featuredImage = product?.FeaturedImagePath;
            var downloadPath = product?.DigitalDownloadPath;

            if (string.IsNullOrWhiteSpace(featuredImage))
            {
                featuredImage = null;
            }

            if (string.IsNullOrWhiteSpace(downloadPath))
            {
                downloadPath = null;
            }

            var dto = new UserPurchasedProductDto(
                invoice.Id,
                invoice.InvoiceNumber,
                item.Id,
                product?.Id,
                name,
                summary,
                product?.Type,
                categoryName,
                item.Quantity,
                item.UnitPrice,
                item.Total,
                purchasedAt,
                featuredImage,
                downloadPath,
                invoice.Status,
                invoice.GrandTotal,
                invoice.PaidAmount,
                invoice.OutstandingAmount);

            result.Add(dto);
        }

        var ordered = result
            .OrderByDescending(dto => dto.PurchasedAt)
            .ThenBy(dto => dto.Name)
            .ToArray();

        return Result<IReadOnlyCollection<UserPurchasedProductDto>>.Success(ordered);
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LogTableRenameTest.Application.Abstractions.Messaging;
using LogTableRenameTest.Application.DTOs.Catalog;
using LogTableRenameTest.Application.Interfaces;
using LogTableRenameTest.Domain.Enums;
using LogTableRenameTest.SharedKernel.BaseTypes;

namespace LogTableRenameTest.Application.Queries.Catalog;

public sealed record GetProductRequestsQuery(
    int PageNumber = 1,
    int PageSize = 20,
    ProductRequestStatus? Status = null,
    string? SellerId = null,
    string? ProductName = null) : IQuery<ProductRequestListResultDto>;

public sealed record ProductRequestListResultDto(
    IReadOnlyCollection<ProductRequestDto> Requests,
    int TotalCount,
    int PageNumber,
    int PageSize);

public sealed record ProductRequestDto(
    Guid Id,
    string Name,
    string Summary,
    ProductType Type,
    decimal? Price,
    Guid CategoryId,
    string CategoryName,
    string? FeaturedImagePath,
    string TagList,
    string SellerId,
    string? SellerName,
    string? SellerPhone,
    ProductRequestStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ReviewedAt,
    string? ReviewerId,
    string? RejectionReason,
    Guid? ApprovedProductId,
    bool IsCustomOrder);

public sealed class GetProductRequestsQueryHandler : IQueryHandler<GetProductRequestsQuery, ProductRequestListResultDto>
{
    private readonly IProductRequestRepository _requestRepository;
    private readonly ISellerProfileRepository _sellerProfileRepository;

    public GetProductRequestsQueryHandler(
        IProductRequestRepository requestRepository,
        ISellerProfileRepository sellerProfileRepository)
    {
        _requestRepository = requestRepository;
        _sellerProfileRepository = sellerProfileRepository;
    }

    public async Task<Result<ProductRequestListResultDto>> Handle(
        GetProductRequestsQuery request,
        CancellationToken cancellationToken)
    {
        var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
        var pageSize = request.PageSize < 1 ? 20 : request.PageSize;

        IReadOnlyCollection<Domain.Entities.Catalog.ProductRequest> requests;

        if (!string.IsNullOrWhiteSpace(request.SellerId))
        {
            requests = await _requestRepository.GetBySellerIdAsync(request.SellerId.Trim(), cancellationToken);
        }
        else if (request.Status.HasValue)
        {
            requests = await _requestRepository.GetByStatusAsync(request.Status.Value, cancellationToken);
        }
        else
        {
            requests = await _requestRepository.GetAllAsync(pageNumber, pageSize, cancellationToken);
        }

        var filteredRequests = requests.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(request.ProductName))
        {
            var productName = request.ProductName.Trim();
            filteredRequests = filteredRequests.Where(r =>
                r.Name.Contains(productName, StringComparison.OrdinalIgnoreCase));
        }

        var filteredRequestsList = filteredRequests.ToList();
        var totalCount = !string.IsNullOrWhiteSpace(request.SellerId) || request.Status.HasValue || !string.IsNullOrWhiteSpace(request.ProductName)
            ? filteredRequestsList.Count
            : await _requestRepository.GetCountAsync(cancellationToken);

        // Apply pagination after filtering
        var paginatedRequests = filteredRequestsList
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var sellerIds = paginatedRequests
            .Where(r => !string.IsNullOrWhiteSpace(r.SellerId))
            .Select(r => r.SellerId!)
            .Distinct()
            .ToArray();

        var sellers = sellerIds.Length > 0
            ? await _sellerProfileRepository.GetAllAsync(cancellationToken)
            : Array.Empty<Domain.Entities.Sellers.SellerProfile>();

        var sellerDict = sellers
            .Where(s => !string.IsNullOrWhiteSpace(s.UserId))
            .ToDictionary(s => s.UserId!, s => new { s.DisplayName, s.ContactPhone });

        var dtos = paginatedRequests.Select(r =>
        {
            var sellerInfo = !string.IsNullOrWhiteSpace(r.SellerId) && sellerDict.TryGetValue(r.SellerId, out var info)
                ? info
                : null;

            return new ProductRequestDto(
                r.Id,
                r.Name,
                r.Summary,
                r.Type,
                r.Price,
                r.CategoryId,
                r.Category?.Name ?? "نامشخص",
                r.FeaturedImagePath,
                r.TagList,
                r.SellerId,
                sellerInfo?.DisplayName,
                sellerInfo?.ContactPhone,
                r.Status,
                r.CreateDate,
                r.ReviewedAt,
                r.ReviewerId,
                r.RejectionReason,
                r.ApprovedProductId,
                r.IsCustomOrder);
        }).ToArray();

        var result = new ProductRequestListResultDto(
            dtos,
            totalCount,
            pageNumber,
            pageSize);

        return Result<ProductRequestListResultDto>.Success(result);
    }
}


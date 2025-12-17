using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TestAttarClone.Application.Abstractions.Messaging;
using TestAttarClone.Application.DTOs.Catalog;
using TestAttarClone.Application.Interfaces;
using TestAttarClone.Domain.Enums;
using TestAttarClone.SharedKernel.BaseTypes;
using Microsoft.EntityFrameworkCore;

namespace TestAttarClone.Application.Queries.Catalog;

public sealed record GetProductCustomRequestsQuery(
    int PageNumber = 1,
    int PageSize = 20,
    CustomRequestStatus? Status = null,
    Guid? ProductId = null) : IQuery<ProductCustomRequestListResultDto>;

public sealed record ProductCustomRequestListResultDto(
    IReadOnlyCollection<ProductCustomRequestDto> Requests,
    int TotalCount,
    int PageNumber,
    int PageSize);

public sealed record ProductCustomRequestDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    string? UserId,
    string FullName,
    string Phone,
    string? Email,
    string? Message,
    CustomRequestStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ContactedAt,
    string? AdminNotes);

public sealed class GetProductCustomRequestsQueryHandler : IQueryHandler<GetProductCustomRequestsQuery, ProductCustomRequestListResultDto>
{
    private readonly IProductCustomRequestRepository _requestRepository;

    public GetProductCustomRequestsQueryHandler(IProductCustomRequestRepository requestRepository)
    {
        _requestRepository = requestRepository;
    }

    public async Task<Result<ProductCustomRequestListResultDto>> Handle(
        GetProductCustomRequestsQuery request,
        CancellationToken cancellationToken)
    {
        var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
        var pageSize = request.PageSize < 1 ? 20 : request.PageSize;

        IReadOnlyCollection<Domain.Entities.Catalog.ProductCustomRequest> requests;

        if (request.ProductId.HasValue)
        {
            requests = await _requestRepository.GetByProductIdAsync(request.ProductId.Value, cancellationToken);
        }
        else if (request.Status.HasValue)
        {
            requests = await _requestRepository.GetByStatusAsync(request.Status.Value, cancellationToken);
        }
        else
        {
            requests = await _requestRepository.GetAllAsync(pageNumber, pageSize, cancellationToken);
        }

        var totalCount = request.ProductId.HasValue || request.Status.HasValue
            ? requests.Count
            : await _requestRepository.GetCountAsync(cancellationToken);

        var dtos = requests.Select(r => new ProductCustomRequestDto(
            r.Id,
            r.ProductId,
            r.Product?.Name ?? "نامشخص",
            r.UserId,
            r.FullName,
            r.Phone,
            r.Email,
            r.Message,
            r.Status,
            r.CreateDate,
            r.ContactedAt,
            r.AdminNotes)).ToArray();

        var result = new ProductCustomRequestListResultDto(
            dtos,
            totalCount,
            pageNumber,
            pageSize);

        return Result<ProductCustomRequestListResultDto>.Success(result);
    }
}


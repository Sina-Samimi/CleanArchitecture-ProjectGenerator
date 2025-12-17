using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LogTableRenameTest.Application.Abstractions.Messaging;
using LogTableRenameTest.Application.DTOs.Billing;
using LogTableRenameTest.Application.Interfaces;
using LogTableRenameTest.Domain.Enums;
using LogTableRenameTest.SharedKernel.BaseTypes;

namespace LogTableRenameTest.Application.Queries.Billing;

public sealed record GetWithdrawalRequestsQuery(
    string? SellerId,
    string? UserId,
    WithdrawalRequestType? RequestType,
    WithdrawalRequestStatus? Status,
    int? PageNumber = null,
    int? PageSize = null) : IQuery<WithdrawalRequestListResultDto>;

public sealed class GetWithdrawalRequestsQueryHandler : IQueryHandler<GetWithdrawalRequestsQuery, WithdrawalRequestListResultDto>
{
    private readonly IWithdrawalRequestRepository _withdrawalRequestRepository;

    public GetWithdrawalRequestsQueryHandler(IWithdrawalRequestRepository withdrawalRequestRepository)
    {
        _withdrawalRequestRepository = withdrawalRequestRepository;
    }

    public async Task<Result<WithdrawalRequestListResultDto>> Handle(GetWithdrawalRequestsQuery request, CancellationToken cancellationToken)
    {
        var pageNumber = request.PageNumber ?? 1;
        var pageSize = request.PageSize ?? 20;

        IReadOnlyCollection<Domain.Entities.Billing.WithdrawalRequest> requests;

        if (!string.IsNullOrWhiteSpace(request.SellerId))
        {
            requests = await _withdrawalRequestRepository.GetBySellerIdAsync(request.SellerId.Trim(), cancellationToken);
        }
        else if (!string.IsNullOrWhiteSpace(request.UserId))
        {
            requests = await _withdrawalRequestRepository.GetByUserIdAsync(request.UserId.Trim(), cancellationToken);
        }
        else
        {
            requests = await _withdrawalRequestRepository.GetAllAsync(request.Status, request.RequestType, cancellationToken);
        }

        // Filter by RequestType if specified
        if (request.RequestType.HasValue && (string.IsNullOrWhiteSpace(request.SellerId) && string.IsNullOrWhiteSpace(request.UserId)))
        {
            requests = requests.Where(r => r.RequestType == request.RequestType.Value).ToList();
        }

        // Filter by Status if specified and not already filtered
        if (request.Status.HasValue && string.IsNullOrWhiteSpace(request.SellerId) && string.IsNullOrWhiteSpace(request.UserId))
        {
            requests = requests.Where(r => r.Status == request.Status.Value).ToList();
        }

        var totalCount = requests.Count;
        var pagedRequests = requests
            .OrderByDescending(r => r.CreateDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var dtos = pagedRequests
            .Select(r => r.ToListItemDto())
            .ToArray();

        var result = new WithdrawalRequestListResultDto(
            dtos,
            totalCount,
            pageNumber,
            pageSize,
            DateTimeOffset.UtcNow);

        return Result<WithdrawalRequestListResultDto>.Success(result);
    }
}


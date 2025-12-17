using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LogsDtoCloneTest.Application.Abstractions.Messaging;
using LogsDtoCloneTest.Application.Interfaces;
using LogsDtoCloneTest.Domain.Enums;
using LogsDtoCloneTest.SharedKernel.BaseTypes;

namespace LogsDtoCloneTest.Application.Queries.Billing;

public sealed record GetSellerTotalWithdrawalsQuery(string SellerId) : IQuery<decimal>;

public sealed class GetSellerTotalWithdrawalsQueryHandler : IQueryHandler<GetSellerTotalWithdrawalsQuery, decimal>
{
    private readonly IWithdrawalRequestRepository _withdrawalRequestRepository;

    public GetSellerTotalWithdrawalsQueryHandler(IWithdrawalRequestRepository withdrawalRequestRepository)
    {
        _withdrawalRequestRepository = withdrawalRequestRepository;
    }

    public async Task<Result<decimal>> Handle(GetSellerTotalWithdrawalsQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.SellerId))
        {
            return Result<decimal>.Failure("شناسه فروشنده معتبر نیست.");
        }

        var requests = await _withdrawalRequestRepository.GetBySellerIdAsync(
            request.SellerId.Trim(),
            cancellationToken);

        // فقط درخواست‌های پردازش شده از نوع سهم فروشنده را محاسبه کن
        var totalWithdrawn = requests
            .Where(r => r.RequestType == WithdrawalRequestType.SellerRevenue &&
                       r.Status == WithdrawalRequestStatus.Processed)
            .Sum(r => r.Amount);

        return Result<decimal>.Success(totalWithdrawn);
    }
}


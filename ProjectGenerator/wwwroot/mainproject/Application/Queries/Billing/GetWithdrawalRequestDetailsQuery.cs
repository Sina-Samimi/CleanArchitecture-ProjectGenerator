using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MobiRooz.Application.Abstractions.Messaging;
using MobiRooz.Application.DTOs.Billing;
using MobiRooz.Application.Interfaces;
using MobiRooz.SharedKernel.BaseTypes;

namespace MobiRooz.Application.Queries.Billing;

public sealed record GetWithdrawalRequestDetailsQuery(Guid RequestId) : IQuery<WithdrawalRequestDetailsDto>;

public sealed class GetWithdrawalRequestDetailsQueryHandler : IQueryHandler<GetWithdrawalRequestDetailsQuery, WithdrawalRequestDetailsDto>
{
    private readonly IWithdrawalRequestRepository _withdrawalRequestRepository;
    private readonly IInvoiceRepository _invoiceRepository;

    public GetWithdrawalRequestDetailsQueryHandler(
        IWithdrawalRequestRepository withdrawalRequestRepository,
        IInvoiceRepository invoiceRepository)
    {
        _withdrawalRequestRepository = withdrawalRequestRepository;
        _invoiceRepository = invoiceRepository;
    }

    public async Task<Result<WithdrawalRequestDetailsDto>> Handle(GetWithdrawalRequestDetailsQuery request, CancellationToken cancellationToken)
    {
        var withdrawalRequest = await _withdrawalRequestRepository.GetByIdAsync(request.RequestId, cancellationToken);

        if (withdrawalRequest is null)
        {
            return Result<WithdrawalRequestDetailsDto>.Failure("درخواست برداشت یافت نشد.");
        }

        // Find invoice by external reference if exists
        Guid? paymentInvoiceId = null;
        var externalReference = $"WITHDRAWAL_REQUEST:{withdrawalRequest.Id}";
        
        // Search for invoice with matching external reference
        var invoiceList = await _invoiceRepository.GetListAsync(
            new InvoiceListFilterDto(
                SearchTerm: null,
                UserId: null,
                Status: null,
                IssueDateFrom: null,
                IssueDateTo: null,
                ExternalReference: externalReference,
                PageNumber: null,
                PageSize: null), 
            cancellationToken);
        
        var matchingInvoice = invoiceList.FirstOrDefault();
        if (matchingInvoice is not null)
        {
            paymentInvoiceId = matchingInvoice.Id;
        }

        return Result<WithdrawalRequestDetailsDto>.Success(withdrawalRequest.ToDetailsDto(paymentInvoiceId));
    }
}


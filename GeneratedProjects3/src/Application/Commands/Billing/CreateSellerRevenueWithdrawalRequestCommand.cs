using System;
using System.Threading;
using System.Threading.Tasks;
using LogTableRenameTest.Application.Abstractions.Messaging;
using LogTableRenameTest.Application.DTOs.Billing;
using LogTableRenameTest.Application.Interfaces;
using LogTableRenameTest.Application.Queries.Billing;
using LogTableRenameTest.Application.Queries.Sellers;
using LogTableRenameTest.Domain.Entities.Billing;
using LogTableRenameTest.Domain.Enums;
using LogTableRenameTest.Domain.Exceptions;
using LogTableRenameTest.SharedKernel.BaseTypes;
using MediatR;

namespace LogTableRenameTest.Application.Commands.Billing;

public sealed record CreateSellerRevenueWithdrawalRequestCommand(
    string SellerId,
    decimal Amount,
    string? Currency,
    string? BankAccountNumber,
    string? CardNumber,
    string? Iban,
    string? BankName,
    string? AccountHolderName,
    string? Description) : ICommand<WithdrawalRequestListItemDto>;

public sealed class CreateSellerRevenueWithdrawalRequestCommandHandler : ICommandHandler<CreateSellerRevenueWithdrawalRequestCommand, WithdrawalRequestListItemDto>
{
    private const string DefaultCurrency = "IRT";

    private readonly IWithdrawalRequestRepository _withdrawalRequestRepository;
    private readonly IAuditContext _auditContext;
    private readonly IMediator _mediator;

    public CreateSellerRevenueWithdrawalRequestCommandHandler(
        IWithdrawalRequestRepository withdrawalRequestRepository,
        IAuditContext auditContext,
        IMediator mediator)
    {
        _withdrawalRequestRepository = withdrawalRequestRepository;
        _auditContext = auditContext;
        _mediator = mediator;
    }

    public async Task<Result<WithdrawalRequestListItemDto>> Handle(CreateSellerRevenueWithdrawalRequestCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.SellerId))
        {
            return Result<WithdrawalRequestListItemDto>.Failure("شناسه فروشنده معتبر نیست.");
        }

        if (request.Amount <= 0)
        {
            return Result<WithdrawalRequestListItemDto>.Failure("مبلغ برداشت باید بیشتر از صفر باشد.");
        }

        var currency = string.IsNullOrWhiteSpace(request.Currency)
            ? DefaultCurrency
            : request.Currency!.Trim().ToUpperInvariant();

        try
        {
            // دریافت درآمد کل و کل برداشت‌های انجام شده
            var totalRevenueQuery = new GetSellerPaymentsQuery(request.SellerId.Trim());
            var totalRevenueResult = await _mediator.Send(totalRevenueQuery, cancellationToken);
            
            if (!totalRevenueResult.IsSuccess || totalRevenueResult.Value is null)
            {
                return Result<WithdrawalRequestListItemDto>.Failure("خطا در دریافت اطلاعات درآمد.");
            }

            var totalWithdrawalsQuery = new GetSellerTotalWithdrawalsQuery(request.SellerId.Trim());
            var totalWithdrawalsResult = await _mediator.Send(totalWithdrawalsQuery, cancellationToken);
            
            if (!totalWithdrawalsResult.IsSuccess)
            {
                return Result<WithdrawalRequestListItemDto>.Failure("خطا در محاسبه کل برداشت‌های انجام شده.");
            }

            var totalRevenue = totalRevenueResult.Value.TotalRevenue;
            var totalWithdrawn = totalWithdrawalsResult.Value;
            var availableAmount = totalRevenue - totalWithdrawn;

            if (availableAmount < request.Amount)
            {
                return Result<WithdrawalRequestListItemDto>.Failure(
                    $"مبلغ درخواست شده ({request.Amount:N0} {currency}) بیشتر از مبلغ قابل برداشت ({availableAmount:N0} {currency}) است. " +
                    $"درآمد کل: {totalRevenue:N0} {currency}، " +
                    $"برداشت شده: {totalWithdrawn:N0} {currency}، " +
                    $"مانده قابل برداشت: {availableAmount:N0} {currency}");
            }

            var audit = _auditContext.Capture();
            var withdrawalRequest = new WithdrawalRequest(
                WithdrawalRequestType.SellerRevenue,
                request.SellerId.Trim(),
                null,
                request.Amount,
                currency,
                request.BankAccountNumber,
                request.CardNumber,
                request.Iban,
                request.BankName,
                request.AccountHolderName,
                request.Description)
            {
                CreatorId = audit.UserId,
                CreateDate = audit.Timestamp,
                UpdateDate = audit.Timestamp,
                UpdaterId = audit.UserId,
                Ip = audit.IpAddress
            };

            await _withdrawalRequestRepository.AddAsync(withdrawalRequest, cancellationToken);

            return Result<WithdrawalRequestListItemDto>.Success(withdrawalRequest.ToListItemDto());
        }
        catch (DomainException ex)
        {
            return Result<WithdrawalRequestListItemDto>.Failure(ex.Message);
        }
    }
}


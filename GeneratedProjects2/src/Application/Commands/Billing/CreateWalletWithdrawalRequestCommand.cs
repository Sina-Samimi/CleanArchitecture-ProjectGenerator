using System;
using System.Threading;
using System.Threading.Tasks;
using LogsDtoCloneTest.Application.Abstractions.Messaging;
using LogsDtoCloneTest.Application.DTOs.Billing;
using LogsDtoCloneTest.Application.Interfaces;
using LogsDtoCloneTest.Application.Queries.Billing;
using LogsDtoCloneTest.Domain.Entities.Billing;
using LogsDtoCloneTest.Domain.Enums;
using LogsDtoCloneTest.Domain.Exceptions;
using LogsDtoCloneTest.SharedKernel.BaseTypes;
using MediatR;

namespace LogsDtoCloneTest.Application.Commands.Billing;

public sealed record CreateWalletWithdrawalRequestCommand(
    string UserId,
    decimal Amount,
    string? Currency,
    string? BankAccountNumber,
    string? CardNumber,
    string? Iban,
    string? BankName,
    string? AccountHolderName,
    string? Description) : ICommand<WithdrawalRequestListItemDto>;

public sealed class CreateWalletWithdrawalRequestCommandHandler : ICommandHandler<CreateWalletWithdrawalRequestCommand, WithdrawalRequestListItemDto>
{
    private const string DefaultCurrency = "IRT";

    private readonly IWithdrawalRequestRepository _withdrawalRequestRepository;
    private readonly IWalletRepository _walletRepository;
    private readonly IAuditContext _auditContext;

    public CreateWalletWithdrawalRequestCommandHandler(
        IWithdrawalRequestRepository withdrawalRequestRepository,
        IWalletRepository walletRepository,
        IAuditContext auditContext)
    {
        _withdrawalRequestRepository = withdrawalRequestRepository;
        _walletRepository = walletRepository;
        _auditContext = auditContext;
    }

    public async Task<Result<WithdrawalRequestListItemDto>> Handle(CreateWalletWithdrawalRequestCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.UserId))
        {
            return Result<WithdrawalRequestListItemDto>.Failure("شناسه کاربر معتبر نیست.");
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
            // Check wallet balance
            var wallet = await _walletRepository.GetByUserIdAsync(request.UserId.Trim(), cancellationToken);
            if (wallet is null)
            {
                return Result<WithdrawalRequestListItemDto>.Failure("کیف پول کاربر یافت نشد.");
            }

            if (!string.Equals(wallet.Currency, currency, StringComparison.OrdinalIgnoreCase))
            {
                return Result<WithdrawalRequestListItemDto>.Failure("واحد پول کیف پول با درخواست مطابقت ندارد.");
            }

            if (wallet.Balance < request.Amount)
            {
                return Result<WithdrawalRequestListItemDto>.Failure("موجودی کیف پول برای این درخواست کافی نیست.");
            }

            if (wallet.IsLocked)
            {
                return Result<WithdrawalRequestListItemDto>.Failure("کیف پول در حالت قفل است.");
            }

            var audit = _auditContext.Capture();
            var withdrawalRequest = new WithdrawalRequest(
                WithdrawalRequestType.Wallet,
                null,
                request.UserId.Trim(),
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


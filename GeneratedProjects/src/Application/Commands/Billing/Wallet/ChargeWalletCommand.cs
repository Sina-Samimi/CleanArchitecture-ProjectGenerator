using System;
using System.Threading;
using System.Threading.Tasks;
using TestAttarClone.Application.Abstractions.Messaging;
using TestAttarClone.Application.DTOs.Billing;
using TestAttarClone.Application.Interfaces;
using TestAttarClone.Domain.Entities.Billing;
using TestAttarClone.Domain.Enums;
using TestAttarClone.Domain.Exceptions;
using TestAttarClone.SharedKernel.BaseTypes;
using TestAttarClone.SharedKernel.Helpers;

namespace TestAttarClone.Application.Commands.Billing.Wallet;

public sealed record ChargeWalletCommand(
    string UserId,
    decimal Amount,
    string? Currency,
    string? Description) : ICommand<WalletTransactionListItemDto>;

public sealed class ChargeWalletCommandHandler : ICommandHandler<ChargeWalletCommand, WalletTransactionListItemDto>
{
    private const string DefaultCurrency = "IRT";

    private readonly IWalletRepository _walletRepository;
    private readonly IAuditContext _auditContext;

    public ChargeWalletCommandHandler(IWalletRepository walletRepository, IAuditContext auditContext)
    {
        _walletRepository = walletRepository;
        _auditContext = auditContext;
    }

    public async Task<Result<WalletTransactionListItemDto>> Handle(ChargeWalletCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.UserId))
        {
            return Result<WalletTransactionListItemDto>.Failure("شناسه کاربر معتبر نیست.");
        }

        if (request.Amount <= 0)
        {
            return Result<WalletTransactionListItemDto>.Failure("مبلغ شارژ کیف پول باید بیشتر از صفر باشد.");
        }

        var currency = string.IsNullOrWhiteSpace(request.Currency)
            ? DefaultCurrency
            : request.Currency!.Trim().ToUpperInvariant();

        try
        {
            var existingAccount = await _walletRepository.GetByUserIdAsync(request.UserId.Trim(), cancellationToken);
            var audit = _auditContext.Capture();

            var isNewAccount = existingAccount is null;
            var account = existingAccount ?? new WalletAccount(request.UserId.Trim(), currency)
            {
                CreatorId = audit.UserId,
                CreateDate = audit.Timestamp,
                UpdateDate = audit.Timestamp,
                Ip = audit.IpAddress,
                UpdaterId = audit.UserId
            };

            if (!isNewAccount && !string.Equals(account.Currency, currency, StringComparison.OrdinalIgnoreCase))
            {
                return Result<WalletTransactionListItemDto>.Failure("واحد پول کیف پول با واحد درخواستی مغایرت دارد.");
            }

            // Note: account audit fields will be set automatically by AuditInterceptor

            var reference = GenerateReference("DEP");
            var transaction = account.Credit(
                request.Amount,
                reference,
                request.Description,
                metadata: null,
                invoiceId: null,
                paymentTransactionId: null,
                TransactionStatus.Succeeded,
                audit.Timestamp);

            // Note: transaction audit fields will be set automatically by AuditInterceptor

            if (isNewAccount)
            {
                await _walletRepository.AddAsync(account, cancellationToken);
            }
            else
            {
                await _walletRepository.UpdateAsync(account, cancellationToken);
            }

            return Result<WalletTransactionListItemDto>.Success(transaction.ToListItemDto());
        }
        catch (DomainException ex)
        {
            return Result<WalletTransactionListItemDto>.Failure(ex.Message);
        }
    }

    private static string GenerateReference(string prefix)
    {
        return ReferenceGenerator.GenerateReadableReference($"WL{prefix}", DateTimeOffset.UtcNow);
    }
}

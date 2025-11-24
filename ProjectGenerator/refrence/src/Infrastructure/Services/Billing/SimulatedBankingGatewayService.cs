using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Arsis.Application.DTOs.Billing;
using Arsis.Application.Interfaces;
using Arsis.Domain.Enums;
using Arsis.SharedKernel.BaseTypes;

namespace Arsis.Infrastructure.Services.Billing;

public sealed class SimulatedBankingGatewayService : IBankingGatewayService
{
    private const string GatewayName = "SimulatedBank";
    private readonly ConcurrentDictionary<string, SimulatedSession> _sessions = new();

    public Task<Result<BankPaymentSessionDto>> CreatePaymentSessionAsync(
        BankPaymentRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var reference = $"BNK-{Guid.NewGuid():N}".ToUpperInvariant();
        var sessionToken = $"STS-{Guid.NewGuid():N}".ToUpperInvariant();
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(30);
        var paymentUrl = new Uri($"https://banking.local/pay/{sessionToken}");

        var session = new SimulatedSession(request, reference, sessionToken, expiresAt, null);
        _sessions[reference] = session;

        var sessionDto = new BankPaymentSessionDto(
            GatewayName,
            reference,
            paymentUrl,
            expiresAt,
            request.Amount,
            request.Currency,
            sessionToken,
            $"پرداخت فاکتور {request.InvoiceNumber}");

        return Task.FromResult(Result<BankPaymentSessionDto>.Success(sessionDto));
    }

    public Task<Result<BankPaymentReceiptDto>> VerifyPaymentAsync(string reference, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_sessions.TryGetValue(reference, out var session))
        {
            return Task.FromResult(Result<BankPaymentReceiptDto>.Failure("نشست پرداخت یافت نشد."));
        }

        if (session.Receipt is { } existingReceipt)
        {
            return Task.FromResult(Result<BankPaymentReceiptDto>.Success(existingReceipt));
        }

        BankPaymentReceiptDto receipt;

        if (session.ExpiresAt < DateTimeOffset.UtcNow)
        {
            receipt = new BankPaymentReceiptDto(
                GatewayName,
                reference,
                TransactionStatus.Failed,
                session.Request.Amount,
                session.Request.Currency,
                DateTimeOffset.UtcNow,
                null,
                "مهلت پرداخت در درگاه بانکی به پایان رسیده است.");
        }
        else
        {
            var processedAt = DateTimeOffset.UtcNow;
            var trackingCode = $"TRX-{Guid.NewGuid():N}".ToUpperInvariant();

            receipt = new BankPaymentReceiptDto(
                GatewayName,
                reference,
                TransactionStatus.Succeeded,
                session.Request.Amount,
                session.Request.Currency,
                processedAt,
                trackingCode,
                "پرداخت با موفقیت تایید شد.");
        }

        var updated = session with { Receipt = receipt };
        _sessions[reference] = updated;

        return Task.FromResult(Result<BankPaymentReceiptDto>.Success(receipt));
    }

    private sealed record SimulatedSession(
        BankPaymentRequest Request,
        string Reference,
        string SessionToken,
        DateTimeOffset ExpiresAt,
        BankPaymentReceiptDto? Receipt);
}

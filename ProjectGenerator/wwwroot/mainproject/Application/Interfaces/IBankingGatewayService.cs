using System.Threading;
using System.Threading.Tasks;
using Attar.Application.DTOs.Billing;
using Attar.SharedKernel.BaseTypes;

namespace Attar.Application.Interfaces;

public interface IBankingGatewayService
{
    Task<Result<BankPaymentSessionDto>> CreatePaymentSessionAsync(BankPaymentRequest request, CancellationToken cancellationToken);

    Task<Result<BankPaymentReceiptDto>> VerifyPaymentAsync(string reference, CancellationToken cancellationToken);
}

using System.Threading;
using System.Threading.Tasks;
using Arsis.Application.DTOs.Billing;
using Arsis.SharedKernel.BaseTypes;

namespace Arsis.Application.Interfaces;

public interface IBankingGatewayService
{
    Task<Result<BankPaymentSessionDto>> CreatePaymentSessionAsync(BankPaymentRequest request, CancellationToken cancellationToken);

    Task<Result<BankPaymentReceiptDto>> VerifyPaymentAsync(string reference, CancellationToken cancellationToken);
}

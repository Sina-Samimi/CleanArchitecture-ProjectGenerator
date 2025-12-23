using System.Threading;
using System.Threading.Tasks;
using MobiRooz.Application.DTOs.Billing;
using MobiRooz.SharedKernel.BaseTypes;

namespace MobiRooz.Application.Interfaces;

public interface IBankingGatewayService
{
    Task<Result<BankPaymentSessionDto>> CreatePaymentSessionAsync(BankPaymentRequest request, CancellationToken cancellationToken);

    Task<Result<BankPaymentReceiptDto>> VerifyPaymentAsync(string reference, CancellationToken cancellationToken);
}

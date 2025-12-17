using System.Threading;
using System.Threading.Tasks;
using TestAttarClone.Application.DTOs.Billing;
using TestAttarClone.SharedKernel.BaseTypes;

namespace TestAttarClone.Application.Interfaces;

public interface IBankingGatewayService
{
    Task<Result<BankPaymentSessionDto>> CreatePaymentSessionAsync(BankPaymentRequest request, CancellationToken cancellationToken);

    Task<Result<BankPaymentReceiptDto>> VerifyPaymentAsync(string reference, CancellationToken cancellationToken);
}

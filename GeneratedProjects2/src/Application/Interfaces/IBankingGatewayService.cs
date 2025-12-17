using System.Threading;
using System.Threading.Tasks;
using LogsDtoCloneTest.Application.DTOs.Billing;
using LogsDtoCloneTest.SharedKernel.BaseTypes;

namespace LogsDtoCloneTest.Application.Interfaces;

public interface IBankingGatewayService
{
    Task<Result<BankPaymentSessionDto>> CreatePaymentSessionAsync(BankPaymentRequest request, CancellationToken cancellationToken);

    Task<Result<BankPaymentReceiptDto>> VerifyPaymentAsync(string reference, CancellationToken cancellationToken);
}

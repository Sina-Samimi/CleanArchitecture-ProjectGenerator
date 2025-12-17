using System.Threading;
using System.Threading.Tasks;
using LogTableRenameTest.Application.DTOs.Billing;
using LogTableRenameTest.SharedKernel.BaseTypes;

namespace LogTableRenameTest.Application.Interfaces;

public interface IBankingGatewayService
{
    Task<Result<BankPaymentSessionDto>> CreatePaymentSessionAsync(BankPaymentRequest request, CancellationToken cancellationToken);

    Task<Result<BankPaymentReceiptDto>> VerifyPaymentAsync(string reference, CancellationToken cancellationToken);
}

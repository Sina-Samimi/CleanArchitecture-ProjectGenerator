using System.Threading;
using System.Threading.Tasks;
using TestAttarClone.Application.Abstractions.Messaging;
using TestAttarClone.Application.DTOs.Settings;
using TestAttarClone.Application.Interfaces;
using TestAttarClone.SharedKernel.BaseTypes;

namespace TestAttarClone.Application.Queries.Admin.PaymentSettings;

public sealed record GetPaymentSettingsQuery : IQuery<PaymentSettingDto>
{
    public sealed class Handler : IQueryHandler<GetPaymentSettingsQuery, PaymentSettingDto>
    {
        private readonly IPaymentSettingRepository _repository;

        public Handler(IPaymentSettingRepository repository)
        {
            _repository = repository;
        }

        public async Task<Result<PaymentSettingDto>> Handle(GetPaymentSettingsQuery request, CancellationToken cancellationToken)
        {
            var setting = await _repository.GetCurrentAsync(cancellationToken);

            if (setting is null)
            {
                return Result<PaymentSettingDto>.Success(new PaymentSettingDto(
                    string.Empty,
                    false,
                    false));
            }

            var dto = new PaymentSettingDto(
                setting.ZarinPalMerchantId,
                setting.ZarinPalIsSandbox,
                setting.IsActive);

            return Result<PaymentSettingDto>.Success(dto);
        }
    }
}

using System.Threading;
using System.Threading.Tasks;
using MobiRooz.Application.Abstractions.Messaging;
using MobiRooz.Application.DTOs.Settings;
using MobiRooz.Application.Interfaces;
using MobiRooz.SharedKernel.BaseTypes;

namespace MobiRooz.Application.Queries.Admin.PaymentSettings;

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

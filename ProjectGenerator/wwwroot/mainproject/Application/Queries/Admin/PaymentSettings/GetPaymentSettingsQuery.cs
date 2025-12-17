using System.Threading;
using System.Threading.Tasks;
using Attar.Application.Abstractions.Messaging;
using Attar.Application.DTOs.Settings;
using Attar.Application.Interfaces;
using Attar.SharedKernel.BaseTypes;

namespace Attar.Application.Queries.Admin.PaymentSettings;

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

using System.Threading;
using System.Threading.Tasks;
using Attar.Application.Abstractions.Messaging;
using Attar.Application.DTOs.Financial;
using Attar.Application.Interfaces;
using Attar.SharedKernel.BaseTypes;

namespace Attar.Application.Queries.Admin.FinancialSettings;

public sealed record GetFinancialSettingsQuery : IQuery<FinancialSettingDto>
{
    public sealed class Handler : IQueryHandler<GetFinancialSettingsQuery, FinancialSettingDto>
    {
        private readonly IFinancialSettingRepository _repository;

        public Handler(IFinancialSettingRepository repository)
        {
            _repository = repository;
        }

        public async Task<Result<FinancialSettingDto>> Handle(GetFinancialSettingsQuery request, CancellationToken cancellationToken)
        {
            var setting = await _repository.GetCurrentAsync(cancellationToken);

            if (setting is null)
            {
                // Return default values when no setting exists
                return Result<FinancialSettingDto>.Success(new FinancialSettingDto(0, 0, 0, 0, Domain.Enums.PlatformCommissionCalculationMethod.Complementary));
            }

            var dto = new FinancialSettingDto(
                setting.SellerProductSharePercentage,
                setting.ValueAddedTaxPercentage,
                setting.PlatformCommissionPercentage,
                setting.AffiliateCommissionPercentage,
                setting.CommissionCalculationMethod);

            return Result<FinancialSettingDto>.Success(dto);
        }
    }
}

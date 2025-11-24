using System.Threading;
using System.Threading.Tasks;
using Arsis.Application.Abstractions.Messaging;
using Arsis.Application.DTOs.Financial;
using Arsis.Application.Interfaces;
using Arsis.SharedKernel.BaseTypes;

namespace Arsis.Application.Queries.Admin.FinancialSettings;

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
                return Result<FinancialSettingDto>.Success(new FinancialSettingDto(0, 0, 0, 0, 0));
            }

            var dto = new FinancialSettingDto(
                setting.TeacherPackageSharePercentage,
                setting.TeacherLiveEventSharePercentage,
                setting.ValueAddedTaxPercentage,
                setting.PlatformCommissionPercentage,
                setting.AffiliateCommissionPercentage);

            return Result<FinancialSettingDto>.Success(dto);
        }
    }
}

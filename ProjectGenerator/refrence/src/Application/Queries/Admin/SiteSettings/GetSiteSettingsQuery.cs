using System.Threading;
using System.Threading.Tasks;
using Arsis.Application.Abstractions.Messaging;
using Arsis.Application.DTOs.Settings;
using Arsis.Application.Interfaces;
using Arsis.SharedKernel.BaseTypes;

namespace Arsis.Application.Queries.Admin.SiteSettings;

public sealed record GetSiteSettingsQuery : IQuery<SiteSettingDto>
{
    public sealed class Handler : IQueryHandler<GetSiteSettingsQuery, SiteSettingDto>
    {
        private readonly ISiteSettingRepository _repository;

        public Handler(ISiteSettingRepository repository)
        {
            _repository = repository;
        }

        public async Task<Result<SiteSettingDto>> Handle(GetSiteSettingsQuery request, CancellationToken cancellationToken)
        {
            var setting = await _repository.GetCurrentAsync(cancellationToken);

            if (setting is null)
            {
                return Result<SiteSettingDto>.Success(new SiteSettingDto(
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty));
            }

            var dto = new SiteSettingDto(
                setting.SiteTitle,
                setting.SiteEmail,
                setting.SupportEmail,
                setting.ContactPhone,
                setting.SupportPhone,
                setting.Address,
                setting.ContactDescription);

            return Result<SiteSettingDto>.Success(dto);
        }
    }
}

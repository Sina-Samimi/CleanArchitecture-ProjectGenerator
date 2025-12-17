using System.Threading;
using System.Threading.Tasks;
using TestAttarClone.Application.Abstractions.Messaging;
using TestAttarClone.Application.DTOs.Settings;
using TestAttarClone.Application.Interfaces;
using TestAttarClone.SharedKernel.BaseTypes;

namespace TestAttarClone.Application.Queries.Admin.SiteSettings;

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
                    string.Empty,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    false));
            }

            var dto = new SiteSettingDto(
                setting.SiteTitle,
                setting.SiteEmail,
                setting.SupportEmail,
                setting.ContactPhone,
                setting.SupportPhone,
                setting.Address,
                setting.ContactDescription,
                setting.LogoPath,
                setting.FaviconPath,
                setting.ShortDescription,
                setting.TermsAndConditions,
                setting.TelegramUrl,
                setting.InstagramUrl,
                setting.WhatsAppUrl,
                setting.LinkedInUrl,
                setting.BannersAsSlider);

            return Result<SiteSettingDto>.Success(dto);
        }
    }
}

using System.Threading;
using System.Threading.Tasks;
using TestAttarClone.Application.Abstractions.Messaging;
using TestAttarClone.Application.DTOs.Settings;
using TestAttarClone.Application.Interfaces;
using TestAttarClone.SharedKernel.BaseTypes;

namespace TestAttarClone.Application.Queries.Admin.AboutSettings;

public sealed record GetAboutSettingsQuery : IQuery<AboutSettingDto>
{
    public sealed class Handler : IQueryHandler<GetAboutSettingsQuery, AboutSettingDto>
    {
        private readonly IAboutSettingRepository _repository;

        public Handler(IAboutSettingRepository repository)
        {
            _repository = repository;
        }

        public async Task<Result<AboutSettingDto>> Handle(GetAboutSettingsQuery request, CancellationToken cancellationToken)
        {
            var setting = await _repository.GetCurrentAsync(cancellationToken);

            if (setting is null)
            {
                return Result<AboutSettingDto>.Success(new AboutSettingDto(
                    string.Empty,
                    string.Empty,
                    null,
                    null,
                    null,
                    null,
                    null));
            }

            var dto = new AboutSettingDto(
                setting.Title,
                setting.Description,
                setting.Vision,
                setting.Mission,
                setting.ImagePath,
                setting.MetaTitle,
                setting.MetaDescription);

            return Result<AboutSettingDto>.Success(dto);
        }
    }
}


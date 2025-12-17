using System.Threading;
using System.Threading.Tasks;
using LogTableRenameTest.Application.Abstractions.Messaging;
using LogTableRenameTest.Application.DTOs.Settings;
using LogTableRenameTest.Application.Interfaces;
using LogTableRenameTest.SharedKernel.BaseTypes;

namespace LogTableRenameTest.Application.Queries.Admin.AboutSettings;

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


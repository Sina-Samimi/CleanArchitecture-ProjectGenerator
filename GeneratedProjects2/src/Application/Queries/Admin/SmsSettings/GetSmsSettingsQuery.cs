using System.Threading;
using System.Threading.Tasks;
using LogsDtoCloneTest.Application.Abstractions.Messaging;
using LogsDtoCloneTest.Application.DTOs.Settings;
using LogsDtoCloneTest.Application.Interfaces;
using LogsDtoCloneTest.SharedKernel.BaseTypes;

namespace LogsDtoCloneTest.Application.Queries.Admin.SmsSettings;

public sealed record GetSmsSettingsQuery : IQuery<SmsSettingDto>
{
    public sealed class Handler : IQueryHandler<GetSmsSettingsQuery, SmsSettingDto>
    {
        private readonly ISmsSettingRepository _repository;

        public Handler(ISmsSettingRepository repository)
        {
            _repository = repository;
        }

        public async Task<Result<SmsSettingDto>> Handle(GetSmsSettingsQuery request, CancellationToken cancellationToken)
        {
            var setting = await _repository.GetCurrentAsync(cancellationToken);

            if (setting is null)
            {
                return Result<SmsSettingDto>.Success(new SmsSettingDto(
                    string.Empty,
                    false));
            }

            var dto = new SmsSettingDto(
                setting.ApiKey,
                setting.IsActive);

            return Result<SmsSettingDto>.Success(dto);
        }
    }
}

using System.Threading;
using System.Threading.Tasks;
using TestAttarClone.Application.Abstractions.Messaging;
using TestAttarClone.Application.DTOs.Settings;
using TestAttarClone.Application.Interfaces;
using TestAttarClone.SharedKernel.BaseTypes;

namespace TestAttarClone.Application.Queries.Admin.SmsSettings;

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

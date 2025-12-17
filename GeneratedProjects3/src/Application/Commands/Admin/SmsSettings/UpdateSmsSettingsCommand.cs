using System.Threading;
using System.Threading.Tasks;
using LogTableRenameTest.Application.Abstractions.Messaging;
using LogTableRenameTest.Application.Interfaces;
using LogTableRenameTest.Domain.Entities.Settings;
using LogTableRenameTest.SharedKernel.BaseTypes;

namespace LogTableRenameTest.Application.Commands.Admin.SmsSettings;

public sealed record UpdateSmsSettingsCommand(
    string ApiKey,
    bool IsActive) : ICommand
{
    public sealed class Handler : ICommandHandler<UpdateSmsSettingsCommand>
    {
        private readonly ISmsSettingRepository _repository;
        private readonly IAuditContext _auditContext;

        public Handler(ISmsSettingRepository repository, IAuditContext auditContext)
        {
            _repository = repository;
            _auditContext = auditContext;
        }

        public async Task<Result> Handle(UpdateSmsSettingsCommand request, CancellationToken cancellationToken)
        {
            var apiKey = request.ApiKey?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return Result.Failure("API Key نمی‌تواند خالی باشد.");
            }

            if (apiKey.Length > 500)
            {
                return Result.Failure("API Key نمی‌تواند بیشتر از ۵۰۰ کاراکتر باشد.");
            }

            var audit = _auditContext.Capture();
            var setting = await _repository.GetCurrentForUpdateAsync(cancellationToken);

            if (setting is null)
            {
                setting = new SmsSetting(
                    apiKey,
                    request.IsActive)
                {
                    CreatorId = audit.UserId,
                    CreateDate = audit.Timestamp,
                    UpdateDate = audit.Timestamp,
                    Ip = audit.IpAddress
                };

                await _repository.AddAsync(setting, cancellationToken);
            }
            else
            {
                setting.Update(
                    apiKey,
                    request.IsActive);

                setting.UpdaterId = audit.UserId;
                setting.UpdateDate = audit.Timestamp;
                setting.Ip = audit.IpAddress;

                await _repository.UpdateAsync(setting, cancellationToken);
            }

            return Result.Success();
        }
    }
}

using System.Threading;
using System.Threading.Tasks;
using LogsDtoCloneTest.Application.Abstractions.Messaging;
using LogsDtoCloneTest.Application.Interfaces;
using LogsDtoCloneTest.Domain.Entities.Settings;
using LogsDtoCloneTest.SharedKernel.BaseTypes;

namespace LogsDtoCloneTest.Application.Commands.Admin.PaymentSettings;

public sealed record UpdatePaymentSettingsCommand(
    string ZarinPalMerchantId,
    bool ZarinPalIsSandbox,
    bool IsActive) : ICommand
{
    public sealed class Handler : ICommandHandler<UpdatePaymentSettingsCommand>
    {
        private readonly IPaymentSettingRepository _repository;
        private readonly IAuditContext _auditContext;

        public Handler(IPaymentSettingRepository repository, IAuditContext auditContext)
        {
            _repository = repository;
            _auditContext = auditContext;
        }

        public async Task<Result> Handle(UpdatePaymentSettingsCommand request, CancellationToken cancellationToken)
        {
            var merchantId = request.ZarinPalMerchantId?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(merchantId))
            {
                return Result.Failure("Merchant ID نمی‌تواند خالی باشد.");
            }

            if (merchantId.Length > 500)
            {
                return Result.Failure("Merchant ID نمی‌تواند بیشتر از ۵۰۰ کاراکتر باشد.");
            }

            var audit = _auditContext.Capture();
            var setting = await _repository.GetCurrentForUpdateAsync(cancellationToken);

            if (setting is null)
            {
                setting = new PaymentSetting(
                    merchantId,
                    request.ZarinPalIsSandbox,
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
                    merchantId,
                    request.ZarinPalIsSandbox,
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

using System.Threading;
using System.Threading.Tasks;
using Arsis.Application.Abstractions.Messaging;
using Arsis.Application.Interfaces;
using Arsis.Domain.Entities.Settings;
using Arsis.SharedKernel.BaseTypes;

namespace Arsis.Application.Commands.Admin.FinancialSettings;

public sealed record UpdateFinancialSettingsCommand(
    decimal TeacherPackageSharePercentage,
    decimal TeacherLiveEventSharePercentage,
    decimal ValueAddedTaxPercentage,
    decimal PlatformCommissionPercentage,
    decimal AffiliateCommissionPercentage) : ICommand
{
    public sealed class Handler : ICommandHandler<UpdateFinancialSettingsCommand>
    {
        private readonly IFinancialSettingRepository _repository;
        private readonly IAuditContext _auditContext;

        public Handler(IFinancialSettingRepository repository, IAuditContext auditContext)
        {
            _repository = repository;
            _auditContext = auditContext;
        }

        public async Task<Result> Handle(UpdateFinancialSettingsCommand request, CancellationToken cancellationToken)
        {
            if (!IsValidPercentage(request.TeacherPackageSharePercentage))
            {
                return Result.Failure("درصد سهم مدرس از فروش پکیج باید بین ۰ تا ۱۰۰ باشد.");
            }

            if (!IsValidPercentage(request.TeacherLiveEventSharePercentage))
            {
                return Result.Failure("درصد سهم مدرس از رویدادهای زنده باید بین ۰ تا ۱۰۰ باشد.");
            }

            if (!IsValidPercentage(request.ValueAddedTaxPercentage))
            {
                return Result.Failure("درصد مالیات بر ارزش افزوده باید بین ۰ تا ۱۰۰ باشد.");
            }

            if (!IsValidPercentage(request.PlatformCommissionPercentage))
            {
                return Result.Failure("درصد کارمزد پلتفرم باید بین ۰ تا ۱۰۰ باشد.");
            }

            if (!IsValidPercentage(request.AffiliateCommissionPercentage))
            {
                return Result.Failure("درصد کمیسیون همکاری در فروش باید بین ۰ تا ۱۰۰ باشد.");
            }

            var audit = _auditContext.Capture();

            var setting = await _repository.GetCurrentForUpdateAsync(cancellationToken);

            if (setting is null)
            {
                setting = new FinancialSetting(
                    request.TeacherPackageSharePercentage,
                    request.TeacherLiveEventSharePercentage,
                    request.ValueAddedTaxPercentage,
                    request.PlatformCommissionPercentage,
                    request.AffiliateCommissionPercentage)
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
                    request.TeacherPackageSharePercentage,
                    request.TeacherLiveEventSharePercentage,
                    request.ValueAddedTaxPercentage,
                    request.PlatformCommissionPercentage,
                    request.AffiliateCommissionPercentage);

                setting.UpdaterId = audit.UserId;
                setting.UpdateDate = audit.Timestamp;
                setting.Ip = audit.IpAddress;

                await _repository.UpdateAsync(setting, cancellationToken);
            }

            return Result.Success();
        }

        private static bool IsValidPercentage(decimal value) => value >= 0 && value <= 100;
    }
}

using System.Threading;
using System.Threading.Tasks;
using LogTableRenameTest.Application.Abstractions.Messaging;
using LogTableRenameTest.Application.Interfaces;
using LogTableRenameTest.Domain.Entities.Settings;
using LogTableRenameTest.Domain.Enums;
using LogTableRenameTest.SharedKernel.BaseTypes;

namespace LogTableRenameTest.Application.Commands.Admin.FinancialSettings;

public sealed record UpdateFinancialSettingsCommand(
    decimal SellerProductSharePercentage,
    decimal ValueAddedTaxPercentage,
    decimal PlatformCommissionPercentage,
    decimal AffiliateCommissionPercentage,
    PlatformCommissionCalculationMethod CommissionCalculationMethod) : ICommand
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
            if (!IsValidPercentage(request.SellerProductSharePercentage))
            {
                return Result.Failure("درصد سهم فروشنده از فروش محصول باید بین ۰ تا ۱۰۰ باشد.");
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
                    request.SellerProductSharePercentage,
                    request.ValueAddedTaxPercentage,
                    request.PlatformCommissionPercentage,
                    request.AffiliateCommissionPercentage,
                    request.CommissionCalculationMethod)
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
                    request.SellerProductSharePercentage,
                    request.ValueAddedTaxPercentage,
                    request.PlatformCommissionPercentage,
                    request.AffiliateCommissionPercentage,
                    request.CommissionCalculationMethod);

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

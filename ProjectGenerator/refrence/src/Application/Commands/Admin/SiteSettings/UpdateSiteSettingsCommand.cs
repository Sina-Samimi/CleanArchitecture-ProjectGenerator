using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using Arsis.Application.Abstractions.Messaging;
using Arsis.Application.Interfaces;
using Arsis.Domain.Entities.Settings;
using Arsis.SharedKernel.BaseTypes;

namespace Arsis.Application.Commands.Admin.SiteSettings;

public sealed record UpdateSiteSettingsCommand(
    string SiteTitle,
    string SiteEmail,
    string SupportEmail,
    string ContactPhone,
    string SupportPhone,
    string Address,
    string ContactDescription) : ICommand
{
    public sealed class Handler : ICommandHandler<UpdateSiteSettingsCommand>
    {
        private const int TitleMaxLength = 200;
        private const int EmailMaxLength = 256;
        private const int PhoneMaxLength = 50;
        private const int AddressMaxLength = 500;
        private const int DescriptionMaxLength = 1000;

        private static readonly EmailAddressAttribute EmailValidator = new();

        private readonly ISiteSettingRepository _repository;
        private readonly IAuditContext _auditContext;

        public Handler(ISiteSettingRepository repository, IAuditContext auditContext)
        {
            _repository = repository;
            _auditContext = auditContext;
        }

        public async Task<Result> Handle(UpdateSiteSettingsCommand request, CancellationToken cancellationToken)
        {
            var siteTitle = request.SiteTitle?.Trim() ?? string.Empty;
            var siteEmail = request.SiteEmail?.Trim() ?? string.Empty;
            var supportEmail = request.SupportEmail?.Trim() ?? string.Empty;
            var contactPhone = request.ContactPhone?.Trim() ?? string.Empty;
            var supportPhone = request.SupportPhone?.Trim() ?? string.Empty;
            var address = request.Address?.Trim() ?? string.Empty;
            var contactDescription = request.ContactDescription?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(siteTitle))
            {
                return Result.Failure("عنوان سایت نمی‌تواند خالی باشد.");
            }

            if (siteTitle.Length > TitleMaxLength)
            {
                return Result.Failure($"عنوان سایت نمی‌تواند بیشتر از {TitleMaxLength} کاراکتر باشد.");
            }

            if (!IsValidEmail(siteEmail))
            {
                return Result.Failure("ایمیل اصلی سایت معتبر نیست.");
            }

            if (!IsValidEmail(supportEmail))
            {
                return Result.Failure("ایمیل پشتیبانی معتبر نیست.");
            }

            if (!IsValidPhone(contactPhone))
            {
                return Result.Failure("شماره تماس اصلی معتبر نیست.");
            }

            if (!IsValidPhone(supportPhone))
            {
                return Result.Failure("شماره تماس پشتیبانی معتبر نیست.");
            }

            if (!string.IsNullOrEmpty(address) && address.Length > AddressMaxLength)
            {
                return Result.Failure($"آدرس نمی‌تواند بیشتر از {AddressMaxLength} کاراکتر باشد.");
            }

            if (!string.IsNullOrEmpty(contactDescription) && contactDescription.Length > DescriptionMaxLength)
            {
                return Result.Failure($"توضیحات تماس نمی‌تواند بیشتر از {DescriptionMaxLength} کاراکتر باشد.");
            }

            var audit = _auditContext.Capture();
            var setting = await _repository.GetCurrentForUpdateAsync(cancellationToken);

            if (setting is null)
            {
                setting = new SiteSetting(
                    siteTitle,
                    siteEmail,
                    supportEmail,
                    contactPhone,
                    supportPhone,
                    address,
                    contactDescription)
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
                    siteTitle,
                    siteEmail,
                    supportEmail,
                    contactPhone,
                    supportPhone,
                    address,
                    contactDescription);

                setting.UpdaterId = audit.UserId;
                setting.UpdateDate = audit.Timestamp;
                setting.Ip = audit.IpAddress;

                await _repository.UpdateAsync(setting, cancellationToken);
            }

            return Result.Success();
        }

        private static bool IsValidEmail(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return true;
            }

            return value.Length <= EmailMaxLength && EmailValidator.IsValid(value);
        }

        private static bool IsValidPhone(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return true;
            }

            return value.Length <= PhoneMaxLength;
        }
    }
}

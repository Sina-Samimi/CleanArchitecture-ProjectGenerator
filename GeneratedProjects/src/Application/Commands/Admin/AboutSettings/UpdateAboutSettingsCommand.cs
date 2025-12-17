using System.Threading;
using System.Threading.Tasks;
using TestAttarClone.Application.Abstractions.Messaging;
using TestAttarClone.Application.Interfaces;
using TestAttarClone.Domain.Entities.Settings;
using TestAttarClone.SharedKernel.BaseTypes;

namespace TestAttarClone.Application.Commands.Admin.AboutSettings;

public sealed record UpdateAboutSettingsCommand(
    string Title,
    string Description,
    string? Vision = null,
    string? Mission = null,
    string? ImagePath = null,
    string? MetaTitle = null,
    string? MetaDescription = null) : ICommand
{
    public sealed class Handler : ICommandHandler<UpdateAboutSettingsCommand>
    {
        private const int TitleMaxLength = 300;
        private const int MetaTitleMaxLength = 200;
        private const int MetaDescriptionMaxLength = 500;

        private readonly IAboutSettingRepository _repository;
        private readonly IAuditContext _auditContext;

        public Handler(IAboutSettingRepository repository, IAuditContext auditContext)
        {
            _repository = repository;
            _auditContext = auditContext;
        }

        public async Task<Result> Handle(UpdateAboutSettingsCommand request, CancellationToken cancellationToken)
        {
            var title = request.Title?.Trim() ?? string.Empty;
            var description = request.Description?.Trim() ?? string.Empty;
            var vision = request.Vision?.Trim();
            var mission = request.Mission?.Trim();
            var imagePath = request.ImagePath?.Trim();
            var metaTitle = request.MetaTitle?.Trim();
            var metaDescription = request.MetaDescription?.Trim();

            if (string.IsNullOrWhiteSpace(title))
            {
                return Result.Failure("عنوان صفحه درباره ما نمی‌تواند خالی باشد.");
            }

            if (title.Length > TitleMaxLength)
            {
                return Result.Failure($"عنوان نمی‌تواند بیشتر از {TitleMaxLength} کاراکتر باشد.");
            }

            if (!string.IsNullOrEmpty(metaTitle) && metaTitle.Length > MetaTitleMaxLength)
            {
                return Result.Failure($"عنوان SEO نمی‌تواند بیشتر از {MetaTitleMaxLength} کاراکتر باشد.");
            }

            if (!string.IsNullOrEmpty(metaDescription) && metaDescription.Length > MetaDescriptionMaxLength)
            {
                return Result.Failure($"توضیحات SEO نمی‌تواند بیشتر از {MetaDescriptionMaxLength} کاراکتر باشد.");
            }

            var audit = _auditContext.Capture();
            var setting = await _repository.GetCurrentForUpdateAsync(cancellationToken);

            if (setting is null)
            {
                setting = new AboutSetting(
                    title,
                    description,
                    vision,
                    mission,
                    imagePath,
                    metaTitle,
                    metaDescription)
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
                    title,
                    description,
                    vision,
                    mission,
                    imagePath,
                    metaTitle,
                    metaDescription);

                setting.UpdaterId = audit.UserId;
                setting.UpdateDate = audit.Timestamp;
                setting.Ip = audit.IpAddress;

                await _repository.UpdateAsync(setting, cancellationToken);
            }

            return Result.Success();
        }
    }
}


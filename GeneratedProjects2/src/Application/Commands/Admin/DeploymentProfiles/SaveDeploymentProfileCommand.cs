using System;
using System.Threading;
using System.Threading.Tasks;
using LogsDtoCloneTest.Application.Abstractions.Messaging;
using LogsDtoCloneTest.Application.Interfaces;
using LogsDtoCloneTest.Domain.Entities.Settings;
using LogsDtoCloneTest.SharedKernel.BaseTypes;

namespace LogsDtoCloneTest.Application.Commands.Admin.DeploymentProfiles;

public sealed record SaveDeploymentProfileCommand(
    Guid? Id,
    string Name,
    string Branch,
    string ServerHost,
    int ServerPort,
    string ServerUser,
    string DestinationPath,
    string ArtifactName,
    bool IsActive,
    string? PreDeployCommand,
    string? PostDeployCommand,
    string? ServiceReloadCommand,
    string? SecretKeyName,
    string? Notes) : ICommand<Guid>
{
    public sealed class Handler : ICommandHandler<SaveDeploymentProfileCommand, Guid>
    {
        private const int NameMaxLength = 200;
        private const int BranchMaxLength = 150;
        private const int HostMaxLength = 200;
        private const int UserMaxLength = 100;
        private const int PathMaxLength = 400;
        private const int ArtifactMaxLength = 200;
        private const int CommandMaxLength = 1000;
        private const int ReloadCommandMaxLength = 400;
        private const int SecretNameMaxLength = 200;
        private const int NotesMaxLength = 1000;

        private readonly IDeploymentProfileRepository _repository;
        private readonly IAuditContext _auditContext;

        public Handler(IDeploymentProfileRepository repository, IAuditContext auditContext)
        {
            _repository = repository;
            _auditContext = auditContext;
        }

        public async Task<Result<Guid>> Handle(SaveDeploymentProfileCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var normalizedName = NormalizeRequired(request.Name, nameof(request.Name), NameMaxLength);
                var normalizedBranch = NormalizeRequired(request.Branch, nameof(request.Branch), BranchMaxLength);
                var normalizedHost = NormalizeRequired(request.ServerHost, nameof(request.ServerHost), HostMaxLength);
                var normalizedUser = NormalizeRequired(request.ServerUser, nameof(request.ServerUser), UserMaxLength);
                var normalizedPath = NormalizeRequired(request.DestinationPath, nameof(request.DestinationPath), PathMaxLength);
                var normalizedArtifact = NormalizeRequired(request.ArtifactName, nameof(request.ArtifactName), ArtifactMaxLength);
                var normalizedPreDeploy = NormalizeOptional(request.PreDeployCommand, CommandMaxLength);
                var normalizedPostDeploy = NormalizeOptional(request.PostDeployCommand, CommandMaxLength);
                var normalizedReload = NormalizeOptional(request.ServiceReloadCommand, ReloadCommandMaxLength);
                var normalizedSecret = NormalizeOptional(request.SecretKeyName, SecretNameMaxLength);
                var normalizedNotes = NormalizeOptional(request.Notes, NotesMaxLength);

                if (request.ServerPort is < 1 or > 65535)
                {
                    return Result<Guid>.Failure("شماره پورت باید بین 1 تا 65535 باشد.");
                }

                Guid? excludeId = request.Id is { } id && id != Guid.Empty ? id : null;

                if (await _repository.ExistsByNameAsync(normalizedName, excludeId, cancellationToken))
                {
                    return Result<Guid>.Failure("پروفایلی با این عنوان از قبل وجود دارد.");
                }

                if (await _repository.ExistsByBranchAsync(normalizedBranch, excludeId, cancellationToken))
                {
                    return Result<Guid>.Failure("برای این برنچ قبلاً پروفایل پابلیش ثبت شده است.");
                }

                var audit = _auditContext.Capture();

                if (excludeId is null)
                {
                    var profile = new DeploymentProfile(
                        normalizedName,
                        normalizedBranch,
                        normalizedHost,
                        request.ServerPort,
                        normalizedUser,
                        normalizedPath,
                        normalizedArtifact,
                        request.IsActive,
                        normalizedPreDeploy,
                        normalizedPostDeploy,
                        normalizedReload,
                        normalizedSecret,
                        normalizedNotes)
                    {
                        CreatorId = audit.UserId,
                        CreateDate = audit.Timestamp,
                        UpdateDate = audit.Timestamp,
                        Ip = audit.IpAddress
                    };

                    await _repository.AddAsync(profile, cancellationToken);
                    return Result<Guid>.Success(profile.Id);
                }

                var existing = await _repository.GetByIdAsync(excludeId.Value, cancellationToken);

                if (existing is null)
                {
                    return Result<Guid>.Failure("پروفایل پابلیش مورد نظر یافت نشد.");
                }

                existing.Update(
                    normalizedName,
                    normalizedBranch,
                    normalizedHost,
                    request.ServerPort,
                    normalizedUser,
                    normalizedPath,
                    normalizedArtifact,
                    request.IsActive,
                    normalizedPreDeploy,
                    normalizedPostDeploy,
                    normalizedReload,
                    normalizedSecret,
                    normalizedNotes);

                existing.UpdaterId = audit.UserId;
                existing.UpdateDate = audit.Timestamp;
                existing.Ip = audit.IpAddress;

                await _repository.UpdateAsync(existing, cancellationToken);

                return Result<Guid>.Success(existing.Id);
            }
            catch (ArgumentOutOfRangeException ex)
            {
                return Result<Guid>.Failure(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return Result<Guid>.Failure(ex.Message);
            }
        }

        private static string NormalizeRequired(string value, string name, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException($"مقدار {name} الزامی است.");
            }

            var trimmed = value.Trim();
            if (trimmed.Length > maxLength)
            {
                throw new ArgumentException($"طول {name} نمی‌تواند بیشتر از {maxLength} کاراکتر باشد.");
            }

            return trimmed;
        }

        private static string? NormalizeOptional(string? value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var trimmed = value.Trim();
            if (trimmed.Length > maxLength)
            {
                throw new ArgumentException($"طول مقدار وارد شده نمی‌تواند بیشتر از {maxLength} کاراکتر باشد.");
            }

            return trimmed;
        }
    }
}

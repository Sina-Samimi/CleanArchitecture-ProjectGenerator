using System;
using System.Threading;
using System.Threading.Tasks;
using LogsDtoCloneTest.Application.Abstractions.Messaging;
using LogsDtoCloneTest.Application.DTOs.Deployment;
using LogsDtoCloneTest.Application.Interfaces;
using LogsDtoCloneTest.SharedKernel.BaseTypes;

namespace LogsDtoCloneTest.Application.Queries.Admin.DeploymentProfiles;

public sealed record GetDeploymentProfileByIdQuery(Guid Id) : IQuery<DeploymentProfileDto>
{
    public sealed class Handler : IQueryHandler<GetDeploymentProfileByIdQuery, DeploymentProfileDto>
    {
        private readonly IDeploymentProfileRepository _repository;

        public Handler(IDeploymentProfileRepository repository)
        {
            _repository = repository;
        }

        public async Task<Result<DeploymentProfileDto>> Handle(
            GetDeploymentProfileByIdQuery request,
            CancellationToken cancellationToken)
        {
            if (request.Id == Guid.Empty)
            {
                return Result<DeploymentProfileDto>.Failure("شناسه پروفایل پابلیش معتبر نیست.");
            }

            var profile = await _repository.GetByIdAsync(request.Id, cancellationToken);

            if (profile is null)
            {
                return Result<DeploymentProfileDto>.Failure("پروفایل پابلیش مورد نظر یافت نشد.");
            }

            var dto = new DeploymentProfileDto(
                profile.Id,
                profile.Name,
                profile.Branch,
                profile.ServerHost,
                profile.ServerPort,
                profile.ServerUser,
                profile.DestinationPath,
                profile.ArtifactName,
                profile.IsActive,
                profile.PreDeployCommand,
                profile.PostDeployCommand,
                profile.ServiceReloadCommand,
                profile.SecretKeyName,
                profile.Notes,
                profile.UpdateDate);

            return Result<DeploymentProfileDto>.Success(dto);
        }
    }
}

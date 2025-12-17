using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Attar.Application.Abstractions.Messaging;
using Attar.Application.DTOs.Deployment;
using Attar.Application.Interfaces;
using Attar.SharedKernel.BaseTypes;

namespace Attar.Application.Queries.Admin.DeploymentProfiles;

public sealed record GetDeploymentProfilesQuery : IQuery<IReadOnlyCollection<DeploymentProfileDto>>
{
    public sealed class Handler : IQueryHandler<GetDeploymentProfilesQuery, IReadOnlyCollection<DeploymentProfileDto>>
    {
        private readonly IDeploymentProfileRepository _repository;

        public Handler(IDeploymentProfileRepository repository)
        {
            _repository = repository;
        }

        public async Task<Result<IReadOnlyCollection<DeploymentProfileDto>>> Handle(
            GetDeploymentProfilesQuery request,
            CancellationToken cancellationToken)
        {
            var profiles = await _repository.GetAllAsync(cancellationToken);

            var dtos = profiles
                .Select(profile => new DeploymentProfileDto(
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
                    profile.UpdateDate))
                .ToArray();

            return Result<IReadOnlyCollection<DeploymentProfileDto>>.Success(dtos);
        }
    }
}

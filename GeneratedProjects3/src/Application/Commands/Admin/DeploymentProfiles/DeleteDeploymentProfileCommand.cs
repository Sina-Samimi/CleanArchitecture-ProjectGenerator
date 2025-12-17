using System;
using System.Threading;
using System.Threading.Tasks;
using LogTableRenameTest.Application.Abstractions.Messaging;
using LogTableRenameTest.Application.Interfaces;
using LogTableRenameTest.SharedKernel.BaseTypes;

namespace LogTableRenameTest.Application.Commands.Admin.DeploymentProfiles;

public sealed record DeleteDeploymentProfileCommand(Guid Id) : ICommand
{
    public sealed class Handler : ICommandHandler<DeleteDeploymentProfileCommand>
    {
        private readonly IDeploymentProfileRepository _repository;

        public Handler(IDeploymentProfileRepository repository)
        {
            _repository = repository;
        }

        public async Task<Result> Handle(DeleteDeploymentProfileCommand request, CancellationToken cancellationToken)
        {
            if (request.Id == Guid.Empty)
            {
                return Result.Failure("شناسه پروفایل پابلیش معتبر نیست.");
            }

            var profile = await _repository.GetByIdAsync(request.Id, cancellationToken);

            if (profile is null)
            {
                return Result.Failure("پروفایل پابلیش مورد نظر یافت نشد.");
            }

            await _repository.DeleteAsync(profile, cancellationToken);
            return Result.Success();
        }
    }
}

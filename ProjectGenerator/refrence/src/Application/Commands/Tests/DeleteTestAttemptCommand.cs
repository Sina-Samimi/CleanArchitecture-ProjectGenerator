using System;
using System.Threading;
using System.Threading.Tasks;
using Arsis.Application.Abstractions.Messaging;
using Arsis.Application.Interfaces;
using Arsis.SharedKernel.BaseTypes;

namespace Arsis.Application.Commands.Tests;

public sealed record DeleteTestAttemptCommand(Guid AttemptId) : ICommand<bool>
{
    public sealed class Handler : ICommandHandler<DeleteTestAttemptCommand, bool>
    {
        private readonly IUserTestAttemptRepository _attemptRepository;
        private readonly ITestResultRepository _resultRepository;

        public Handler(
            IUserTestAttemptRepository attemptRepository,
            ITestResultRepository resultRepository)
        {
            _attemptRepository = attemptRepository;
            _resultRepository = resultRepository;
        }

        public async Task<Result<bool>> Handle(DeleteTestAttemptCommand request, CancellationToken cancellationToken)
        {
            var attempt = await _attemptRepository.GetByIdAsync(request.AttemptId, cancellationToken);
            if (attempt is null)
            {
                return Result<bool>.Failure("تلاش مورد نظر یافت نشد.");
            }

            await _resultRepository.DeleteByAttemptIdAsync(attempt.Id, cancellationToken);
            await _attemptRepository.DeleteAsync(attempt, cancellationToken);

            return Result<bool>.Success(true);
        }
    }
}

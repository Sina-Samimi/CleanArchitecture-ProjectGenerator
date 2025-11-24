using System;
using System.Threading;
using System.Threading.Tasks;
using Arsis.Application.Abstractions.Messaging;
using Arsis.Application.Interfaces;
using Arsis.SharedKernel.BaseTypes;

namespace Arsis.Application.Commands.Tests;

public sealed record DeleteTestCommand(Guid Id) : ICommand<bool>
{
    public sealed class Handler : ICommandHandler<DeleteTestCommand, bool>
    {
        private readonly ITestRepository _testRepository;

        public Handler(ITestRepository testRepository)
        {
            _testRepository = testRepository;
        }

        public async Task<Result<bool>> Handle(DeleteTestCommand request, CancellationToken cancellationToken)
        {
            var test = await _testRepository.GetByIdAsync(request.Id, cancellationToken);
            if (test is null)
            {
                return Result<bool>.Failure("تست مورد نظر یافت نشد.");
            }

            await _testRepository.DeleteAsync(test, cancellationToken);

            return Result<bool>.Success(true);
        }
    }
}

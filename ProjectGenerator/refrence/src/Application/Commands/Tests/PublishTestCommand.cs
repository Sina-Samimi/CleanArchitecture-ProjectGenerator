using System;
using System.Threading;
using System.Threading.Tasks;
using Arsis.Application.Abstractions.Messaging;
using Arsis.Application.Interfaces;
using Arsis.SharedKernel.BaseTypes;

namespace Arsis.Application.Commands.Tests;

public sealed record PublishTestCommand(Guid Id) : ICommand<bool>
{
    public sealed class Handler : ICommandHandler<PublishTestCommand, bool>
    {
        private readonly ITestRepository _testRepository;

        public Handler(ITestRepository testRepository)
        {
            _testRepository = testRepository;
        }

        public async Task<Result<bool>> Handle(PublishTestCommand request, CancellationToken cancellationToken)
        {
            var test = await _testRepository.GetByIdWithQuestionsAsync(request.Id, cancellationToken);
            if (test is null)
            {
                return Result<bool>.Failure("تست مورد نظر یافت نشد.");
            }

            try
            {
                test.Publish();
                await _testRepository.UpdateAsync(test, cancellationToken);
                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                return Result<bool>.Failure(ex.Message);
            }
        }
    }
}

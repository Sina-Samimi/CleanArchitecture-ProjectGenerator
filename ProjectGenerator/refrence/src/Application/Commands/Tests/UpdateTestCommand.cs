using System;
using System.Threading;
using System.Threading.Tasks;
using Arsis.Application.Abstractions.Messaging;
using Arsis.Application.Interfaces;
using Arsis.Domain.Enums;
using Arsis.Domain.Exceptions;
using Arsis.SharedKernel.BaseTypes;

namespace Arsis.Application.Commands.Tests;

    public sealed record UpdateTestCommand(
        Guid Id,
        string Title,
        string Description,
        Guid? CategoryId,
        decimal Price,
        string Currency,
        int? DurationMinutes,
        int? MaxAttempts,
        bool ShowResultsImmediately,
        bool ShowCorrectAnswers,
        bool RandomizeQuestions,
        bool RandomizeOptions,
        DateTimeOffset? AvailableFrom,
        DateTimeOffset? AvailableUntil,
        int? NumberOfQuestionsToShow,
        decimal? PassingScore,
        TestType Type,
        TestStatus Status) : ICommand<bool>
{
    public sealed class Handler : ICommandHandler<UpdateTestCommand, bool>
    {
        private readonly ITestRepository _testRepository;
        private readonly IAuditContext _auditContext;

        public Handler(ITestRepository testRepository, IAuditContext auditContext)
        {
            _testRepository = testRepository;
            _auditContext = auditContext;
        }

        public async Task<Result<bool>> Handle(UpdateTestCommand request, CancellationToken cancellationToken)
        {
            var test = await _testRepository.GetByIdAsync(request.Id, cancellationToken);
            if (test is null)
            {
                return Result<bool>.Failure("تست مورد نظر یافت نشد.");
            }

            if (string.IsNullOrWhiteSpace(request.Title))
            {
                return Result<bool>.Failure("عنوان تست الزامی است.");
            }

            var audit = _auditContext.Capture();

            test.SetTitle(request.Title);
            test.SetDescription(request.Description);
            test.SetType(request.Type);
            test.SetCategory(request.CategoryId);
            test.SetPrice(request.Price);
            test.SetCurrency(string.IsNullOrWhiteSpace(request.Currency) ? "IRT" : request.Currency);
            test.SetDuration(request.DurationMinutes);
            test.SetAvailability(request.AvailableFrom, request.AvailableUntil);

            try
            {
                if (test.Status != request.Status)
                {
                    switch (request.Status)
                    {
                        case TestStatus.Draft:
                            test.UnPublish();
                            break;
                        case TestStatus.Published:
                            test.Publish();
                            break;
                        case TestStatus.Archived:
                            test.Archive();
                            break;
                    }
                }
            }
            catch (DomainException ex)
            {
                return Result<bool>.Failure(ex.Message);
            }

            test.UpdaterId = audit.UserId;
            test.UpdateDate = audit.Timestamp;
            test.Ip = audit.IpAddress;

            await _testRepository.UpdateAsync(test, cancellationToken);

            return Result<bool>.Success(true);
        }
    }
}

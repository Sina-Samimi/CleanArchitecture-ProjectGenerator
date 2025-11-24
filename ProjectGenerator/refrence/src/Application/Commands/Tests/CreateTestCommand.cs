using System;
using System.Threading;
using System.Threading.Tasks;
using Arsis.Application.Abstractions.Messaging;
using Arsis.Application.Interfaces;
using Arsis.Domain.Entities.Tests;
using Arsis.Domain.Enums;
using Arsis.SharedKernel.BaseTypes;

namespace Arsis.Application.Commands.Tests;

public sealed record CreateTestCommand(
    string Title,
    string Description,
    TestType Type,
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
    decimal? PassingScore) : ICommand<Guid>
{
    public sealed class Handler : ICommandHandler<CreateTestCommand, Guid>
    {
        private readonly ITestRepository _testRepository;
        private readonly IAuditContext _auditContext;

        public Handler(ITestRepository testRepository, IAuditContext auditContext)
        {
            _testRepository = testRepository;
            _auditContext = auditContext;
        }

        public async Task<Result<Guid>> Handle(CreateTestCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Title))
            {
                return Result<Guid>.Failure("عنوان تست الزامی است.");
            }

            if (request.Price < 0)
            {
                return Result<Guid>.Failure("قیمت نمی‌تواند منفی باشد.");
            }

            if (request.DurationMinutes.HasValue && request.DurationMinutes.Value <= 0)
            {
                return Result<Guid>.Failure("مدت زمان تست باید بزرگتر از صفر باشد.");
            }

            var audit = _auditContext.Capture();

            var test = new Test(
                request.Title,
                request.Description,
                request.Type,
                request.Price,
                string.IsNullOrWhiteSpace(request.Currency) ? "IRT" : request.Currency,
                request.CategoryId,
                request.DurationMinutes,
                request.MaxAttempts,
                request.ShowResultsImmediately,
                request.ShowCorrectAnswers,
                request.RandomizeQuestions,
                request.RandomizeOptions,
                request.NumberOfQuestionsToShow,
                request.PassingScore)
            {
                CreatorId = audit.UserId,
                CreateDate = audit.Timestamp,
                UpdateDate = audit.Timestamp,
                Ip = audit.IpAddress
            };

            test.SetAvailability(request.AvailableFrom, request.AvailableUntil);

            await _testRepository.AddAsync(test, cancellationToken);

            return Result<Guid>.Success(test.Id);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Arsis.Application.Abstractions.Messaging;
using Arsis.Application.Assessments;
using Arsis.Application.Interfaces;
using Arsis.Domain.Entities.Assessments;
using Arsis.Domain.Entities.Tests;
using Arsis.Domain.Enums;
using Arsis.SharedKernel.BaseTypes;

namespace Arsis.Application.Commands.Tests;

public sealed record StartTestAttemptCommand(
    Guid TestId,
    string UserId,
    Guid? InvoiceId) : ICommand<Guid>
{
    public sealed class Handler : ICommandHandler<StartTestAttemptCommand, Guid>
    {
        private readonly ITestRepository _testRepository;
        private readonly IUserTestAttemptRepository _attemptRepository;
        private readonly IInvoiceRepository _invoiceRepository;
        private readonly IAssessmentQuestionRepository _assessmentQuestionRepository;
        private readonly IAuditContext _auditContext;
        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false
        };

        public Handler(
            ITestRepository testRepository,
            IUserTestAttemptRepository attemptRepository,
            IInvoiceRepository invoiceRepository,
            IAssessmentQuestionRepository assessmentQuestionRepository,
            IAuditContext auditContext)
        {
            _testRepository = testRepository;
            _attemptRepository = attemptRepository;
            _invoiceRepository = invoiceRepository;
            _assessmentQuestionRepository = assessmentQuestionRepository;
            _auditContext = auditContext;
        }

        public async Task<Result<Guid>> Handle(StartTestAttemptCommand request, CancellationToken cancellationToken)
        {
            var test = await _testRepository.GetByIdWithQuestionsAsync(request.TestId, cancellationToken);
            if (test is null)
            {
                return Result<Guid>.Failure("تست مورد نظر یافت نشد.");
            }

            if (test.Type == TestType.CliftonSchwartz)
            {
                await EnsureCliftonSchwartzQuestionsAsync(test, cancellationToken).ConfigureAwait(false);
            }

            if (!test.IsAvailable(DateTimeOffset.UtcNow))
            {
                return Result<Guid>.Failure("این تست در حال حاضر در دسترس نیست.");
            }

            // Prevent starting a new attempt while another one is in progress
            var activeAttempt = await _attemptRepository.GetActiveAttemptAsync(
                request.UserId,
                request.TestId,
                cancellationToken);

            if (activeAttempt is not null)
            {
                return Result<Guid>.Failure("شما یک آزمون در حال انجام دارید.");
            }

            // If test has a price, verify payment
            if (test.Price > 0)
            {
                if (!request.InvoiceId.HasValue)
                {
                    return Result<Guid>.Failure("برای شرکت در این تست باید هزینه آن را پرداخت کنید.");
                }

                var invoice = await _invoiceRepository.GetByIdAsync(request.InvoiceId.Value, cancellationToken);
                if (invoice is null || invoice.Status != InvoiceStatus.Paid || invoice.UserId != request.UserId)
                {
                    return Result<Guid>.Failure("فاکتور پرداخت معتبر نیست.");
                }

                // Check if this invoice has already been used for this test
                var attemptWithInvoice = await _attemptRepository.GetByInvoiceIdAsync(
                    request.InvoiceId.Value,
                    request.UserId,
                    cancellationToken);

                if (attemptWithInvoice is not null)
                {
                    if (attemptWithInvoice.Status == TestAttemptStatus.Completed)
                    {
                        return Result<Guid>.Failure("این فاکتور قبلاً برای این تست استفاده شده است.");
                    }

                    if (attemptWithInvoice.Status == TestAttemptStatus.InProgress)
                    {
                        return Result<Guid>.Failure("شما یک آزمون در حال انجام دارید.");
                    }

                    // For statuses like Cancelled/Expired allow creating a fresh attempt with the same invoice
                }
            }
            else
            {
                // For free tests, check MaxAttempts limit
                if (!test.CanUserAttempt(request.UserId))
                {
                    return Result<Guid>.Failure("شما به حداکثر تعداد دفعات شرکت در این تست رسیده‌اید.");
                }
            }

            var attemptCount = await _attemptRepository.GetUserAttemptCountAsync(
                request.UserId,
                request.TestId,
                TestAttemptStatus.Completed,
                cancellationToken);

            var audit = _auditContext.Capture();

            var attempt = new UserTestAttempt(
                test,
                request.UserId,
                attemptCount + 1,
                request.InvoiceId,
                test.DurationMinutes)
            {
                CreatorId = audit.UserId,
                CreateDate = audit.Timestamp,
                UpdateDate = audit.Timestamp,
                Ip = audit.IpAddress
            };

            await _attemptRepository.AddAsync(attempt, cancellationToken);

            return Result<Guid>.Success(attempt.Id);
        }

        private async Task EnsureCliftonSchwartzQuestionsAsync(Test test, CancellationToken cancellationToken)
        {
            var cliftonQuestions = await _assessmentQuestionRepository
                .GetByTestTypeAsync(AssessmentTestType.Clifton, cancellationToken)
                .ConfigureAwait(false);

            var pvqQuestions = await _assessmentQuestionRepository
                .GetByTestTypeAsync(AssessmentTestType.Pvq, cancellationToken)
                .ConfigureAwait(false);

            var expectedCount = cliftonQuestions.Count + pvqQuestions.Count;
            if (expectedCount == 0)
            {
                return;
            }

            if (test.Questions.Count == expectedCount)
            {
                return;
            }

            if (test.Questions.Count > 0)
            {
                foreach (var existing in test.Questions.ToList())
                {
                    test.RemoveQuestion(existing.Id);
                }
            }

            var order = 1;

            foreach (var source in cliftonQuestions.OrderBy(q => q.Index))
            {
                var metadata = new AssessmentQuestionMetadata
                {
                    Assessment = "clifton",
                    Item = $"CLF-{source.Index}",
                    Min = 1,
                    Max = 2,
                    DimensionA = NormalizeTalentCode(source.TalentCodeA),
                    DimensionB = NormalizeTalentCode(source.TalentCodeB)
                };

                var explanation = JsonSerializer.Serialize(metadata, SerializerOptions);
                var question = test.AddQuestion(
                    text: source.TextA is not null && source.TextB is not null
                        ? "کدام گزینه توصیف‌کننده شماست؟"
                        : (source.TextA ?? source.TextB ?? $"سوال {source.Index}"),
                    questionType: TestQuestionType.MultipleChoice,
                    order: order++,
                    score: null,
                    isRequired: true,
                    imageUrl: null,
                    explanation: explanation);

                var optionA = question.AddOption(source.TextA ?? "گزینه اول");
                optionA.SetOrder(1);
                optionA.SetExplanation(NormalizeTalentCode(source.TalentCodeA));

                var optionB = question.AddOption(source.TextB ?? "گزینه دوم");
                optionB.SetOrder(2);
                optionB.SetExplanation(NormalizeTalentCode(source.TalentCodeB));

                _testRepository.AddQuestionGraph(question);
            }

            foreach (var source in pvqQuestions.OrderBy(q => q.Index))
            {
                var likertMin = source.LikertMin ?? 1;
                var likertMax = source.LikertMax ?? 6;

                var metadata = new AssessmentQuestionMetadata
                {
                    Assessment = "pvq",
                    Dimension = NormalizePvqCode(source.PvqCode),
                    Item = $"PVQ-{source.Index}",
                    Min = likertMin,
                    Max = likertMax,
                    MinLabel = "کاملاً مخالفم",
                    MaxLabel = "کاملاً موافقم"
                };

                var explanation = JsonSerializer.Serialize(metadata, SerializerOptions);
                var question = test.AddQuestion(
                    text: source.Text ?? $"سوال PVQ {source.Index}",
                    questionType: TestQuestionType.LikertScale,
                    order: order++,
                    score: null,
                    isRequired: true,
                    imageUrl: null,
                    explanation: explanation);

                _testRepository.AddQuestionGraph(question);
            }

            await _testRepository.UpdateAsync(test, cancellationToken).ConfigureAwait(false);
        }

        private static string? NormalizeTalentCode(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return null;
            }

            var trimmed = raw.Trim().ToUpperInvariant();
            return trimmed.StartsWith("E", StringComparison.OrdinalIgnoreCase) ? trimmed : $"E{trimmed}";
        }

        private static string? NormalizePvqCode(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return null;
            }

            var trimmed = raw.Trim().ToUpperInvariant();
            return trimmed.StartsWith("A", StringComparison.OrdinalIgnoreCase) ? trimmed : $"A{trimmed}";
        }
    }
}

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
using Microsoft.Extensions.Logging;

namespace Arsis.Application.Commands.Tests;

public sealed record CompleteTestAttemptCommand(Guid AttemptId) : ICommand<bool>
{
    public sealed class Handler : ICommandHandler<CompleteTestAttemptCommand, bool>
    {
        private readonly IUserTestAttemptRepository _attemptRepository;
        private readonly ITestRepository _testRepository;
        private readonly IAssessmentService _assessmentService;
        private readonly ITestResultRepository _resultRepository;
        private readonly IAssessmentQuestionRepository _assessmentQuestionRepository;
        private readonly ILogger<Handler> _logger;

        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false
        };

        public Handler(
            IUserTestAttemptRepository attemptRepository,
            ITestRepository testRepository,
            IAssessmentService assessmentService,
            ITestResultRepository resultRepository,
            IAssessmentQuestionRepository assessmentQuestionRepository,
            ILogger<Handler> logger)
        {
            _attemptRepository = attemptRepository;
            _testRepository = testRepository;
            _assessmentService = assessmentService;
            _resultRepository = resultRepository;
            _assessmentQuestionRepository = assessmentQuestionRepository;
            _logger = logger;
        }

        public async Task<Result<bool>> Handle(CompleteTestAttemptCommand request, CancellationToken cancellationToken)
        {
            var attempt = await _attemptRepository.GetByIdWithAnswersAsync(
                request.AttemptId,
                cancellationToken,
                includeDetails: false,
                asTracking: true);
            if (attempt is null)
            {
                return Result<bool>.Failure("آزمون مورد نظر یافت نشد.");
            }

            var test = await _testRepository.GetByIdWithQuestionsAsync(
                attempt.TestId,
                cancellationToken,
                asTracking: false);
            if (test is null)
            {
                return Result<bool>.Failure("تست مورد نظر یافت نشد.");
            }

            _logger.LogInformation("Completing test attempt {AttemptId} for test {TestId} ({TestType})", 
                attempt.Id, test.Id, test.Type);
            _logger.LogInformation("Test has {QuestionCount} questions, Attempt has {AnswerCount} answers", 
                test.Questions.Count, attempt.Answers.Count);

            // For CliftonSchwartz, ensure questions are properly loaded with metadata
            if (test.Type == TestType.CliftonSchwartz && test.Questions.Count == 0)
            {
                _logger.LogWarning("CliftonSchwartz test {TestId} has no questions, ensuring questions...", test.Id);

                var trackedTest = await _testRepository.GetByIdWithQuestionsAsync(
                    attempt.TestId,
                    cancellationToken,
                    asTracking: true);

                if (trackedTest is null)
                {
                    _logger.LogWarning("Unable to reload test {TestId} in tracking mode to ensure questions", test.Id);
                }
                else if (trackedTest.Questions.Count == 0)
                {
                    await EnsureCliftonSchwartzQuestionsAsync(trackedTest, cancellationToken).ConfigureAwait(false);

                    test = await _testRepository.GetByIdWithQuestionsAsync(
                        attempt.TestId,
                        cancellationToken,
                        asTracking: false) ?? test;

                    _logger.LogInformation("After ensuring: Test now has {QuestionCount} questions", test.Questions.Count);
                }
                else
                {
                    test = await _testRepository.GetByIdWithQuestionsAsync(
                        attempt.TestId,
                        cancellationToken,
                        asTracking: false) ?? test;
                }
            }

            try
            {
                attempt.Complete();

                switch (test.Type)
                {
                    case TestType.General:
                        CalculateGeneralScore(attempt, test);
                        break;
                    case TestType.CliftonSchwartz:
                        await EvaluateCliftonSchwartzAsync(attempt, test, cancellationToken).ConfigureAwait(false);
                        break;
                }

                await _attemptRepository.SaveChangesAsync(cancellationToken);

                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to complete test attempt {AttemptId}", attempt.Id);
                return Result<bool>.Failure(ex.Message);
            }
        }

        private static void CalculateGeneralScore(UserTestAttempt attempt, Test test)
        {
            decimal totalScore = 0;
            decimal maxScore = 0;

            foreach (var question in test.Questions)
            {
                var answer = attempt.Answers.FirstOrDefault(a => a.QuestionId == question.Id);

                // If question doesn't have a score, use default value of 1
                decimal questionScore = question.Score ?? 1;

                maxScore += questionScore;

                if (answer?.SelectedOptionId.HasValue == true)
                {
                    var selectedOption = question.Options.FirstOrDefault(o => o.Id == answer.SelectedOptionId.Value);
                    if (selectedOption?.IsCorrect == true)
                    {
                        totalScore += questionScore;
                        answer.SetCorrectness(true);
                        answer.SetScore(questionScore);
                    }
                    else
                    {
                        answer.SetCorrectness(false);
                        answer.SetScore(0);
                    }
                }
                else if (answer != null)
                {
                    // User answered but didn't select an option (shouldn't happen in MultipleChoice, but handle it)
                    answer.SetCorrectness(false);
                    answer.SetScore(0);
                }
            }

            attempt.CalculateScore(totalScore, maxScore, test.PassingScore);
        }

        private async Task EvaluateCliftonSchwartzAsync(UserTestAttempt attempt, Test test, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting Clifton + Schwartz evaluation for attempt {AttemptId}", attempt.Id);
            _logger.LogInformation("Test has {QuestionCount} questions, Attempt has {AnswerCount} answers", 
                test.Questions.Count, attempt.Answers.Count);
            
            var request = BuildAssessmentRequest(attempt, test);
            if (request is null)
            {
                var errorMessage = $"خطا در ایجاد درخواست تحلیل: metadata سوالات ناقص است. (تعداد سوالات: {test.Questions.Count}, تعداد پاسخ‌ها: {attempt.Answers.Count})";
                _logger.LogWarning("Skipping Clifton + Schwartz evaluation for attempt {AttemptId}: metadata missing. Questions: {QuestionCount}, Answers: {AnswerCount}", 
                    attempt.Id, test.Questions.Count, attempt.Answers.Count);
                
                // Store error as a result so user knows what happened
                var errorResult = new TestResult(
                    attempt,
                    "EvaluationError",
                    "خطا در تحلیل آزمون",
                    errorMessage,
                    0m,
                    null,
                    null);
                
                await _resultRepository.AddAsync(errorResult, cancellationToken).ConfigureAwait(false);
                return;
            }

            try
            {
                var response = await _assessmentService.EvaluateAsync(request, cancellationToken).ConfigureAwait(false);
                var payload = JsonSerializer.Serialize(response, SerializerOptions);

                var summaryResult = new TestResult(
                    attempt,
                    "CliftonSchwartzResponse",
                    "نتایج آزمون Clifton + Schwartz",
                    "گزارش ترکیبی استعدادها و ارزش‌ها",
                    0m,
                    null,
                    payload);

                await _resultRepository.AddAsync(summaryResult, cancellationToken).ConfigureAwait(false);
                
                _logger.LogInformation("Successfully evaluated Clifton + Schwartz assessment for attempt {AttemptId}", attempt.Id);
            }
            catch (Exception ex)
            {
                var errorMessage = $"خطا در محاسبه تحلیل: {ex.Message}";
                _logger.LogError(ex, "Failed to evaluate Clifton + Schwartz assessment for attempt {AttemptId}", attempt.Id);
                
                // Store error as a result so user knows what happened
                var errorResult = new TestResult(
                    attempt,
                    "EvaluationError",
                    "خطا در تحلیل آزمون",
                    errorMessage,
                    0m,
                    null,
                    JsonSerializer.Serialize(new { error = ex.Message, type = ex.GetType().Name }, SerializerOptions));
                
                await _resultRepository.AddAsync(errorResult, cancellationToken).ConfigureAwait(false);
            }
        }

        private static AssessmentRequest? BuildAssessmentRequest(UserTestAttempt attempt, Test test)
        {
            var clifton = new Dictionary<string, IDictionary<string, int>>(StringComparer.OrdinalIgnoreCase);
            var pvq = new Dictionary<string, IDictionary<string, int>>(StringComparer.OrdinalIgnoreCase);
            var hasData = false;
            string? inventoryId = null;
            var metadataFoundCount = 0;
            var answeredQuestions = 0;

            foreach (var question in test.Questions)
            {
                if (!TryGetMetadata(question, out var metadata))
                {
                    continue;
                }
                
                metadataFoundCount++;

                var answer = attempt.Answers.FirstOrDefault(a => a.QuestionId == question.Id);
                if (answer is null)
                {
                    continue;
                }
                
                answeredQuestions++;

                if (metadata.Assessment is not null &&
                    metadata.Assessment.Equals("clifton", StringComparison.OrdinalIgnoreCase) &&
                    metadata.DimensionA is not null &&
                    metadata.DimensionB is not null)
                {
                    var selectedOption = question.Options.FirstOrDefault(o => o.Id == answer?.SelectedOptionId);

                    var dimensionA = metadata.DimensionA;
                    var dimensionB = metadata.DimensionB;
                    var explanation = selectedOption?.Explanation?.Trim();

                    var valueA = string.Equals(explanation, dimensionA, StringComparison.OrdinalIgnoreCase) ? 2 : 1;
                    var valueB = string.Equals(explanation, dimensionB, StringComparison.OrdinalIgnoreCase) ? 2 : 1;

                    var itemKeyA = metadata.Item is not null ? $"{metadata.Item}-A" : $"{question.Id:N}-A";
                    var itemKeyB = metadata.Item is not null ? $"{metadata.Item}-B" : $"{question.Id:N}-B";

                    AddValue(clifton, dimensionA, itemKeyA, valueA);
                    AddValue(clifton, dimensionB, itemKeyB, valueB);
                    hasData = true;
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(metadata.InventoryId) && string.IsNullOrWhiteSpace(inventoryId))
                {
                    inventoryId = metadata.InventoryId;
                }

                 answer = attempt.Answers.FirstOrDefault(a => a.QuestionId == question.Id);
                if (answer is null)
                {
                    continue;
                }

                var numericValue = ExtractNumericAnswer(answer, question);
                if (!numericValue.HasValue)
                {
                    continue;
                }

                var dimensionKey = metadata.Dimension ?? question.Id.ToString("N");
                var itemKey = metadata.Item ?? question.Id.ToString("N");

                var clampedValue = numericValue.Value;
                if (metadata.Min.HasValue)
                {
                    clampedValue = Math.Max(metadata.Min.Value, clampedValue);
                }

                if (metadata.Max.HasValue)
                {
                    clampedValue = Math.Min(metadata.Max.Value, clampedValue);
                }

                if (IsClifton(metadata))
                {
                    AddValue(clifton, dimensionKey, itemKey, clampedValue);
                    hasData = true;
                }
                else if (IsPvq(metadata))
                {
                    AddValue(pvq, dimensionKey, itemKey, clampedValue);
                    hasData = true;
                }
            }

            if (!hasData)
            {
                Console.WriteLine($"⚠️ BuildAssessmentRequest returning null:");
                Console.WriteLine($"   - Total questions: {test.Questions.Count}");
                Console.WriteLine($"   - Metadata found: {metadataFoundCount}");
                Console.WriteLine($"   - Answered questions: {answeredQuestions}");
                Console.WriteLine($"   - Clifton dimensions: {clifton.Count}");
                Console.WriteLine($"   - PVQ dimensions: {pvq.Count}");
                return null;
            }

            var userNumericId = ParseUserId(attempt.UserId, attempt.Id);

            return new AssessmentRequest
            {
                UserId = userNumericId,
                InventoryId = inventoryId ?? attempt.Id.ToString("N"),
                Cilifton = clifton,
                Pvq = pvq
            };
        }

        private static bool TryGetMetadata(TestQuestion question, out AssessmentQuestionMetadata metadata)
        {
            metadata = null!;

            if (string.IsNullOrWhiteSpace(question.Explanation))
            {
                return false;
            }

            try
            {
                var parsed = JsonSerializer.Deserialize<AssessmentQuestionMetadata>(question.Explanation, SerializerOptions);
                if (parsed is null || string.IsNullOrWhiteSpace(parsed.Assessment))
                {
                    return false;
                }

                metadata = parsed;
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool IsClifton(AssessmentQuestionMetadata metadata) =>
            metadata.Assessment is not null &&
            metadata.Assessment.Equals("clifton", StringComparison.OrdinalIgnoreCase);

        private static bool IsPvq(AssessmentQuestionMetadata metadata) =>
            metadata.Assessment is not null &&
            metadata.Assessment.Equals("pvq", StringComparison.OrdinalIgnoreCase);

        private static void AddValue(IDictionary<string, IDictionary<string, int>> lookup, string key, string item, int value)
        {
            if (!lookup.TryGetValue(key, out var inner))
            {
                inner = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                lookup[key] = inner;
            }

            inner[item] = value;
        }

        private static int? ExtractNumericAnswer(UserTestAnswer answer, TestQuestion question)
        {
            if (answer.LikertValue.HasValue)
            {
                return answer.LikertValue.Value;
            }

            if (answer.SelectedOption is not null)
            {
                if (answer.SelectedOption.Score.HasValue)
                {
                    return answer.SelectedOption.Score.Value;
                }

                if (int.TryParse(answer.SelectedOption.Text, out var optionParsed))
                {
                    return optionParsed;
                }
            }

            if (answer.SelectedOptionId.HasValue)
            {
                var option = question.Options.FirstOrDefault(o => o.Id == answer.SelectedOptionId.Value);
                if (option is not null)
                {
                    if (option.Score.HasValue)
                    {
                        return option.Score.Value;
                    }

                    if (int.TryParse(option.Text, out var optionTextParsed))
                    {
                        return optionTextParsed;
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(answer.TextAnswer) && int.TryParse(answer.TextAnswer.Trim(), out var textParsed))
            {
                return textParsed;
            }

            return null;
        }

        private static long ParseUserId(string? userId, Guid attemptId)
        {
            if (!string.IsNullOrWhiteSpace(userId) && long.TryParse(userId, out var parsed))
            {
                return parsed;
            }

            var bytes = attemptId.ToByteArray();
            var numeric = Math.Abs(BitConverter.ToInt64(bytes, 0));
            return numeric == 0 ? 1 : numeric;
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
                _logger.LogWarning("No assessment questions found in database for CliftonSchwartz");
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

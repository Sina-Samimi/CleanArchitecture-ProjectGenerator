using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arsis.Application.Abstractions.Messaging;
using Arsis.Application.Interfaces;
using Arsis.Domain.Entities.Tests;
using Arsis.SharedKernel.BaseTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Arsis.Application.Commands.Tests;

public sealed record SubmitTestAnswerCommand(
    Guid AttemptId,
    Guid QuestionId,
    Guid? SelectedOptionId,
    string? TextAnswer,
    int? LikertValue) : ICommand<bool>
{
    public sealed class Handler : ICommandHandler<SubmitTestAnswerCommand, bool>
    {
        private readonly IUserTestAttemptRepository _attemptRepository;
        private readonly ILogger<Handler> _logger;

        public Handler(IUserTestAttemptRepository attemptRepository, ILogger<Handler> logger)
        {
            _attemptRepository = attemptRepository;
            _logger = logger;
        }

        public async Task<Result<bool>> Handle(SubmitTestAnswerCommand request, CancellationToken cancellationToken)
        {
            const int maxRetries = 5;
            
            _logger.LogInformation("📝 [START] Saving answer for QuestionId={QuestionId}", request.QuestionId);
            
            for (int retryCount = 0; retryCount < maxRetries; retryCount++)
            {
                try
                {
                    _logger.LogInformation("🔍 [STEP 1] Loading attempt {AttemptId} (retry {Retry})", request.AttemptId, retryCount);
                    
                    // Get the attempt with answers (needed for AddAnswer to check duplicates)
                    var attempt = await _attemptRepository.GetByIdWithAnswersAsync(request.AttemptId, cancellationToken);
                    
                    if (attempt is null)
                    {
                        _logger.LogError("❌ [FAIL] Attempt not found: {AttemptId}", request.AttemptId);
                        return Result<bool>.Failure("آزمون مورد نظر یافت نشد.");
                    }

                    _logger.LogInformation("✅ [STEP 2] Attempt loaded. Status={Status}, AnswerCount={Count}, ExpiresAt={Expires}, StartedAt={Started}", 
                        attempt.Status, attempt.Answers.Count, attempt.ExpiresAt, attempt.StartedAt);

                    // Check status but don't fail - let domain handle it
                    if (attempt.Status != Domain.Enums.TestAttemptStatus.InProgress)
                    {
                        _logger.LogWarning("⚠️ [WARNING] Status is {Status}, not InProgress. Will attempt to add answer anyway (domain will validate).", 
                            attempt.Status);
                    }

                    // Add the answer using domain method (handles duplicates automatically)
                    _logger.LogInformation("➕ [STEP 3] Adding answer for QuestionId={QuestionId}", request.QuestionId);
                    
                    UserTestAnswer addedAnswer;
                    try
                    {
                        addedAnswer = attempt.AddAnswer(
                            request.QuestionId,
                            request.SelectedOptionId,
                            request.TextAnswer,
                            request.LikertValue);
                    }
                    catch (Exception domainEx)
                    {
                        _logger.LogError(domainEx, "❌ [FAIL] Domain error while adding answer. Status={Status}, Message={Message}", 
                            attempt.Status, domainEx.Message);
                        throw;
                    }

                    _logger.LogInformation("✅ [STEP 4] Answer added. New AnswerCount={Count}, AnswerId={AnswerId}", 
                        attempt.Answers.Count, addedAnswer.Id);

                    // Save ONLY the new answer, not the entire attempt with all previous answers
                    // This prevents rollback of all answers if one fails
                    _logger.LogInformation("💾 [STEP 5] Saving only the new answer to database...");
                    
                    await _attemptRepository.UpdateAsync(attempt, cancellationToken);

                    _logger.LogInformation("🎉 [SUCCESS] Answer saved successfully! AnswerId={AnswerId}", addedAnswer.Id);
                    return Result<bool>.Success(true);
                }
                catch (DbUpdateConcurrencyException ex) when (retryCount < maxRetries - 1)
                {
                    _logger.LogWarning("⚠️ [RETRY] Concurrency conflict (retry {RetryCount}/{MaxRetries}) for QuestionId={QuestionId}: {Error}", 
                        retryCount + 1, maxRetries, request.QuestionId, ex.Message);
                    
                    // Exponential backoff: 20ms, 40ms, 80ms, 160ms, 320ms
                    await Task.Delay(20 * (int)Math.Pow(2, retryCount), cancellationToken);
                    
                    // Continue to next iteration (retry)
                }
                catch (DbUpdateException ex)
                {
                    _logger.LogError(ex, "❌ [FAIL] Database update error for AttemptId={AttemptId}, QuestionId={QuestionId}\nInnerException: {Inner}", 
                        request.AttemptId, request.QuestionId, ex.InnerException?.Message);

                    if (IsUniqueAnswerViolation(ex))
                    {
                        _logger.LogWarning("🔁 [RETRY] Detected duplicate answer for AttemptId={AttemptId}, QuestionId={QuestionId}. Trying to update existing answer instead.", 
                            request.AttemptId, request.QuestionId);

                        var duplicateHandled = await TryUpdateExistingAnswerAsync(request, cancellationToken);
                        if (duplicateHandled)
                        {
                            _logger.LogInformation("✅ [RECOVERED] Duplicate answer updated successfully for AttemptId={AttemptId}, QuestionId={QuestionId}", 
                                request.AttemptId, request.QuestionId);
                            return Result<bool>.Success(true);
                        }

                        _logger.LogWarning("⚠️ [RECOVERY FAILED] Unable to update existing answer after duplicate violation for AttemptId={AttemptId}, QuestionId={QuestionId}", 
                            request.AttemptId, request.QuestionId);
                    }

                    return Result<bool>.Failure("خطا در ذخیره پاسخ.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ [FAIL] Unexpected error for AttemptId={AttemptId}, QuestionId={QuestionId}\nType: {Type}\nMessage: {Message}\nStackTrace: {Stack}", 
                        request.AttemptId, request.QuestionId, ex.GetType().Name, ex.Message, ex.StackTrace);
                    return Result<bool>.Failure(ex.Message);
                }
            }

            _logger.LogError("❌ [FAIL] Failed to save answer after {MaxRetries} retries for QuestionId={QuestionId}", 
                maxRetries, request.QuestionId);
            return Result<bool>.Failure("خطا در ذخیره پاسخ بعد از چندین تلاش.");
        }

        private static bool IsUniqueAnswerViolation(DbUpdateException exception)
        {
            var message = exception.InnerException?.Message ?? exception.Message;
            if (string.IsNullOrWhiteSpace(message))
            {
                return false;
            }

            return message.Contains("IX_UserTestAnswers_AttemptId_QuestionId", StringComparison.OrdinalIgnoreCase)
                || message.Contains("duplicate", StringComparison.OrdinalIgnoreCase)
                || message.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase);
        }

        private async Task<bool> TryUpdateExistingAnswerAsync(SubmitTestAnswerCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var attempt = await _attemptRepository.GetByIdWithAnswersAsync(
                    request.AttemptId,
                    cancellationToken,
                    includeDetails: false,
                    asTracking: true);

                if (attempt is null)
                {
                    _logger.LogWarning("⚠️ [RECOVERY] Attempt {AttemptId} not found while trying to handle duplicate answer.", request.AttemptId);
                    return false;
                }

                var existingAnswer = attempt.Answers.FirstOrDefault(answer => answer.QuestionId == request.QuestionId);
                if (existingAnswer is null)
                {
                    _logger.LogWarning("⚠️ [RECOVERY] Existing answer not found for AttemptId={AttemptId}, QuestionId={QuestionId}.", 
                        request.AttemptId, request.QuestionId);
                    return false;
                }

                existingAnswer.UpdateAnswer(request.SelectedOptionId, request.TextAnswer, request.LikertValue);
                await _attemptRepository.SaveChangesAsync(cancellationToken);
                return true;
            }
            catch (Exception retryException)
            {
                _logger.LogError(retryException, "❌ [RECOVERY] Failed to update existing answer after duplicate violation for AttemptId={AttemptId}, QuestionId={QuestionId}", 
                    request.AttemptId, request.QuestionId);
                return false;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arsis.Application.Abstractions.Messaging;
using Arsis.Application.Interfaces;
using Arsis.Domain.Enums;
using Arsis.SharedKernel.BaseTypes;

namespace Arsis.Application.Commands.Tests;

public sealed record UpdateTestQuestionCommand(
    Guid TestId,
    Guid QuestionId,
    string Text,
    TestQuestionType QuestionType,
    int Order,
    int? Score,
    bool IsRequired,
    string? ImageUrl,
    string? Explanation,
    List<QuestionOptionInput>? Options) : ICommand<bool>
{
    public sealed class Handler : ICommandHandler<UpdateTestQuestionCommand, bool>
    {
        private readonly ITestRepository _testRepository;
        private readonly IAuditContext _auditContext;

        public Handler(ITestRepository testRepository, IAuditContext auditContext)
        {
            _testRepository = testRepository;
            _auditContext = auditContext;
        }

        public async Task<Result<bool>> Handle(UpdateTestQuestionCommand request, CancellationToken cancellationToken)
        {
            var test = await _testRepository.GetByIdWithQuestionsAsync(request.TestId, cancellationToken);
            if (test is null)
            {
                return Result<bool>.Failure("تست مورد نظر یافت نشد.");
            }

            var question = test.Questions.FirstOrDefault(q => q.Id == request.QuestionId);
            if (question is null)
            {
                return Result<bool>.Failure("سوال مورد نظر یافت نشد.");
            }

            if (string.IsNullOrWhiteSpace(request.Text))
            {
                return Result<bool>.Failure("متن سوال الزامی است.");
            }

            var audit = _auditContext.Capture();

            // Update question properties
            question.SetQuestionType(request.QuestionType);
            question.SetText(request.Text);
            question.SetOrder(request.Order);
            question.SetScore(request.Score);
            question.SetIsRequired(request.IsRequired);
            question.SetImageUrl(request.ImageUrl);
            question.SetExplanation(request.Explanation);

            var existingOptions = question.Options.ToDictionary(o => o.Id);
            var optionOrder = 0;

            if (request.Options is not null && request.Options.Count > 0)
            {
                foreach (var optionInput in request.Options)
                {
                    if (string.IsNullOrWhiteSpace(optionInput.Text))
                    {
                        continue;
                    }

                    if (optionInput.Id.HasValue && existingOptions.TryGetValue(optionInput.Id.Value, out var existingOption))
                    {
                        existingOption.SetText(optionInput.Text);
                        existingOption.SetIsCorrect(optionInput.IsCorrect);
                        existingOption.SetScore(optionInput.Score);
                        existingOption.SetImageUrl(optionInput.ImageUrl);
                        existingOption.SetExplanation(optionInput.Explanation);
                        existingOption.SetOrder(optionOrder++);
                        existingOptions.Remove(optionInput.Id.Value);
                    }
                    else
                    {
                        var newOption = question.AddOption(
                            optionInput.Text,
                            optionInput.IsCorrect,
                            optionInput.Score,
                            optionInput.ImageUrl,
                            optionInput.Explanation);

                        newOption.SetOrder(optionOrder++);
                    }
                }
            }

            foreach (var optionToRemove in existingOptions.Values)
            {
                question.RemoveOption(optionToRemove.Id);
            }

            // Update test audit info
            test.UpdaterId = audit.UserId;
            test.UpdateDate = audit.Timestamp;
            test.Ip = audit.IpAddress;

            // Save changes
            try
            {
                await _testRepository.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                return Result<bool>.Failure($"خطا در ذخیره‌سازی: {ex.Message}");
            }

            return Result<bool>.Success(true);
        }
    }
}

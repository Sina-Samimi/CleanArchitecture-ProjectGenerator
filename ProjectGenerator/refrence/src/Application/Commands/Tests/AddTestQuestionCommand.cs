using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Arsis.Application.Abstractions.Messaging;
using Arsis.Application.Interfaces;
using Arsis.Domain.Enums;
using Arsis.SharedKernel.BaseTypes;

namespace Arsis.Application.Commands.Tests;

public sealed record AddTestQuestionCommand(
    Guid TestId,
    string Text,
    TestQuestionType QuestionType,
    int Order,
    int? Score,
    bool IsRequired,
    string? ImageUrl,
    string? Explanation,
    List<QuestionOptionInput>? Options) : ICommand<Guid>
{
    public sealed class Handler : ICommandHandler<AddTestQuestionCommand, Guid>
    {
        private readonly ITestRepository _testRepository;
        private readonly IAuditContext _auditContext;

        public Handler(ITestRepository testRepository, IAuditContext auditContext)
        {
            _testRepository = testRepository;
            _auditContext = auditContext;
        }

        public async Task<Result<Guid>> Handle(AddTestQuestionCommand request, CancellationToken cancellationToken)
        {
            var test = await _testRepository.GetByIdWithQuestionsAsync(request.TestId, cancellationToken);
            if (test is null)
            {
                return Result<Guid>.Failure("تست مورد نظر یافت نشد.");
            }

            if (string.IsNullOrWhiteSpace(request.Text))
            {
                return Result<Guid>.Failure("متن سوال الزامی است.");
            }

            var audit = _auditContext.Capture();

            var question = test.AddQuestion(
                request.Text,
                request.QuestionType,
                request.Order,
                request.Score,
                request.IsRequired,
                request.ImageUrl,
                request.Explanation);

            // Set audit fields for question
            question.CreatorId = audit.UserId;
            question.CreateDate = audit.Timestamp;
            question.UpdateDate = audit.Timestamp;
            question.Ip = audit.IpAddress;

            // Add options if any
            if (request.Options is not null && request.Options.Count > 0)
            {
                int optionOrder = 0;
                foreach (var optionInput in request.Options)
                {
                    if (string.IsNullOrWhiteSpace(optionInput.Text))
                        continue;

                    var option = question.AddOption(
                        optionInput.Text,
                        optionInput.IsCorrect,
                        optionInput.Score,
                        optionInput.ImageUrl,
                        optionInput.Explanation);

                    option.SetOrder(optionOrder++);
                    option.CreatorId = audit.UserId;
                    option.CreateDate = audit.Timestamp;
                    option.UpdateDate = audit.Timestamp;
                    option.Ip = audit.IpAddress;
                }
            }

            // Update test audit info - این را بعد از اضافه کردن سوال انجام می‌دهیم
            test.UpdaterId = audit.UserId;
            test.UpdateDate = audit.Timestamp;
            test.Ip = audit.IpAddress;

            _testRepository.AddQuestionGraph(question);

            // Save changes
            try
            {
                await _testRepository.SaveChangesAsync(cancellationToken);
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException ex)
            {
                return Result<Guid>.Failure($"خطا در ذخیره‌سازی: {ex.Message}. لطفاً دوباره تلاش کنید.");
            }
            catch (Exception ex)
            {
                return Result<Guid>.Failure($"خطا در ذخیره‌سازی: {ex.Message}");
            }

            return Result<Guid>.Success(question.Id);
        }
    }
}

public sealed record QuestionOptionInput(
    Guid? Id,
    string Text,
    bool IsCorrect,
    int? Score,
    string? ImageUrl,
    string? Explanation = null);

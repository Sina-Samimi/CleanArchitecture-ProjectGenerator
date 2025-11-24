using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arsis.Application.Abstractions.Messaging;
using Arsis.Application.Interfaces;
using Arsis.SharedKernel.BaseTypes;

namespace Arsis.Application.Commands.Tests;

public sealed record DeleteTestQuestionCommand(
    Guid TestId,
    Guid QuestionId) : ICommand<bool>
{
    public sealed class Handler : ICommandHandler<DeleteTestQuestionCommand, bool>
    {
        private readonly ITestRepository _testRepository;
        private readonly IAuditContext _auditContext;

        public Handler(ITestRepository testRepository, IAuditContext auditContext)
        {
            _testRepository = testRepository;
            _auditContext = auditContext;
        }

        public async Task<Result<bool>> Handle(DeleteTestQuestionCommand request, CancellationToken cancellationToken)
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

            var audit = _auditContext.Capture();

            test.RemoveQuestion(request.QuestionId);

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

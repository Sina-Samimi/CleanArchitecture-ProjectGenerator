using System;
using System.Threading;
using System.Threading.Tasks;
using LogsDtoCloneTest.Application.Abstractions.Messaging;
using LogsDtoCloneTest.Application.Interfaces;
using LogsDtoCloneTest.SharedKernel.BaseTypes;

namespace LogsDtoCloneTest.Application.Commands.Catalog;

public sealed record CreateProductFaqCommand(
    Guid ProductId,
    string Question,
    string Answer,
    int DisplayOrder) : ICommand<Guid>
{
    private const int MaxQuestionLength = 300;
    private const int MaxAnswerLength = 2000;

    public sealed class Handler : ICommandHandler<CreateProductFaqCommand, Guid>
    {
        private readonly IProductRepository _productRepository;
        private readonly IAuditContext _auditContext;

        public Handler(IProductRepository productRepository, IAuditContext auditContext)
        {
            _productRepository = productRepository;
            _auditContext = auditContext;
        }

        public async Task<Result<Guid>> Handle(CreateProductFaqCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Question))
            {
                return Result<Guid>.Failure("عنوان سوال الزامی است.");
            }

            if (string.IsNullOrWhiteSpace(request.Answer))
            {
                return Result<Guid>.Failure("پاسخ سوال الزامی است.");
            }

            var question = request.Question.Trim();
            if (question.Length > MaxQuestionLength)
            {
                return Result<Guid>.Failure("عنوان سوال نمی‌تواند بیش از ۳۰۰ کاراکتر باشد.");
            }

            var answer = request.Answer.Trim();
            if (answer.Length > MaxAnswerLength)
            {
                return Result<Guid>.Failure("پاسخ سوال نمی‌تواند بیش از ۲۰۰۰ کاراکتر باشد.");
            }

            if (request.DisplayOrder < 0)
            {
                return Result<Guid>.Failure("ترتیب نمایش نمی‌تواند منفی باشد.");
            }

            var product = await _productRepository.GetWithDetailsAsync(request.ProductId, cancellationToken);
            if (product is null)
            {
                return Result<Guid>.Failure("محصول مورد نظر یافت نشد.");
            }

            var faq = product.AddFaq(question, answer, request.DisplayOrder);

            var audit = _auditContext.Capture();

            faq.CreatorId = audit.UserId;
            faq.CreateDate = audit.Timestamp;
            faq.UpdaterId = audit.UserId;
            faq.UpdateDate = audit.Timestamp;
            faq.Ip = audit.IpAddress;

            product.UpdaterId = audit.UserId;
            product.UpdateDate = audit.Timestamp;
            product.Ip = audit.IpAddress;

            await _productRepository.UpdateAsync(product, cancellationToken);

            return Result<Guid>.Success(faq.Id);
        }
    }
}

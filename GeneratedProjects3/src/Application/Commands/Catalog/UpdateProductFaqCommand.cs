using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LogTableRenameTest.Application.Abstractions.Messaging;
using LogTableRenameTest.Application.Interfaces;
using LogTableRenameTest.SharedKernel.BaseTypes;

namespace LogTableRenameTest.Application.Commands.Catalog;

public sealed record UpdateProductFaqCommand(
    Guid ProductId,
    Guid FaqId,
    string Question,
    string Answer,
    int DisplayOrder) : ICommand
{
    private const int MaxQuestionLength = 300;
    private const int MaxAnswerLength = 2000;

    public sealed class Handler : ICommandHandler<UpdateProductFaqCommand>
    {
        private readonly IProductRepository _productRepository;
        private readonly IAuditContext _auditContext;

        public Handler(IProductRepository productRepository, IAuditContext auditContext)
        {
            _productRepository = productRepository;
            _auditContext = auditContext;
        }

        public async Task<Result> Handle(UpdateProductFaqCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Question))
            {
                return Result.Failure("عنوان سوال الزامی است.");
            }

            if (string.IsNullOrWhiteSpace(request.Answer))
            {
                return Result.Failure("پاسخ سوال الزامی است.");
            }

            var question = request.Question.Trim();
            if (question.Length > MaxQuestionLength)
            {
                return Result.Failure("عنوان سوال نمی‌تواند بیش از ۳۰۰ کاراکتر باشد.");
            }

            var answer = request.Answer.Trim();
            if (answer.Length > MaxAnswerLength)
            {
                return Result.Failure("پاسخ سوال نمی‌تواند بیش از ۲۰۰۰ کاراکتر باشد.");
            }

            if (request.DisplayOrder < 0)
            {
                return Result.Failure("ترتیب نمایش نمی‌تواند منفی باشد.");
            }

            var product = await _productRepository.GetWithDetailsAsync(request.ProductId, cancellationToken);
            if (product is null)
            {
                return Result.Failure("محصول مورد نظر یافت نشد.");
            }

            var faq = product.Faqs.FirstOrDefault(item => item.Id == request.FaqId && !item.IsDeleted);
            if (faq is null)
            {
                return Result.Failure("سوال انتخاب شده یافت نشد.");
            }

            faq.UpdateDetails(question, answer, request.DisplayOrder);

            var audit = _auditContext.Capture();

            faq.UpdaterId = audit.UserId;
            faq.UpdateDate = audit.Timestamp;
            faq.Ip = audit.IpAddress;

            product.UpdaterId = audit.UserId;
            product.UpdateDate = audit.Timestamp;
            product.Ip = audit.IpAddress;

            await _productRepository.UpdateAsync(product, cancellationToken);

            return Result.Success();
        }
    }
}

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TestAttarClone.Application.Abstractions.Messaging;
using TestAttarClone.Application.Interfaces;
using TestAttarClone.SharedKernel.BaseTypes;

namespace TestAttarClone.Application.Commands.Catalog;

public sealed record UpdateProductExecutionStepCommand(
    Guid ProductId,
    Guid StepId,
    string Title,
    string? Description,
    string? Duration,
    int DisplayOrder) : ICommand
{
    private const int MaxTitleLength = 200;
    private const int MaxDescriptionLength = 1000;
    private const int MaxDurationLength = 100;

    public sealed class Handler : ICommandHandler<UpdateProductExecutionStepCommand>
    {
        private readonly IProductRepository _productRepository;
        private readonly IAuditContext _auditContext;

        public Handler(IProductRepository productRepository, IAuditContext auditContext)
        {
            _productRepository = productRepository;
            _auditContext = auditContext;
        }

        public async Task<Result> Handle(UpdateProductExecutionStepCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Title))
            {
                return Result.Failure("عنوان گام الزامی است.");
            }

            var title = request.Title.Trim();

            if (title.Length > MaxTitleLength)
            {
                return Result.Failure("عنوان گام نمی‌تواند بیش از ۲۰۰ کاراکتر باشد.");
            }

            var description = string.IsNullOrWhiteSpace(request.Description)
                ? null
                : request.Description.Trim();

            if (description is not null && description.Length > MaxDescriptionLength)
            {
                return Result.Failure("توضیحات گام نمی‌تواند بیش از ۱۰۰۰ کاراکتر باشد.");
            }

            var duration = string.IsNullOrWhiteSpace(request.Duration)
                ? null
                : request.Duration.Trim();

            if (duration is not null && duration.Length > MaxDurationLength)
            {
                return Result.Failure("مدت زمان گام نمی‌تواند بیش از ۱۰۰ کاراکتر باشد.");
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

            var step = product.ExecutionSteps.FirstOrDefault(item => item.Id == request.StepId && !item.IsDeleted);
            if (step is null)
            {
                return Result.Failure("گام انتخاب شده یافت نشد.");
            }

            step.UpdateDetails(title, description, duration, request.DisplayOrder);

            var audit = _auditContext.Capture();

            step.UpdaterId = audit.UserId;
            step.UpdateDate = audit.Timestamp;
            step.Ip = audit.IpAddress;

            product.UpdaterId = audit.UserId;
            product.UpdateDate = audit.Timestamp;
            product.Ip = audit.IpAddress;

            await _productRepository.UpdateAsync(product, cancellationToken);

            return Result.Success();
        }
    }
}

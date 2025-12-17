using System;
using System.Threading;
using System.Threading.Tasks;
using Attar.Application.Abstractions.Messaging;
using Attar.Application.Interfaces;
using Attar.SharedKernel.BaseTypes;

namespace Attar.Application.Commands.Catalog;

public sealed record CreateProductExecutionStepCommand(
    Guid ProductId,
    string Title,
    string? Description,
    string? Duration,
    int DisplayOrder) : ICommand<Guid>
{
    private const int MaxTitleLength = 200;
    private const int MaxDescriptionLength = 1000;
    private const int MaxDurationLength = 100;

    public sealed class Handler : ICommandHandler<CreateProductExecutionStepCommand, Guid>
    {
        private readonly IProductRepository _productRepository;
        private readonly IAuditContext _auditContext;

        public Handler(IProductRepository productRepository, IAuditContext auditContext)
        {
            _productRepository = productRepository;
            _auditContext = auditContext;
        }

        public async Task<Result<Guid>> Handle(CreateProductExecutionStepCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Title))
            {
                return Result<Guid>.Failure("عنوان گام الزامی است.");
            }

            var title = request.Title.Trim();

            if (title.Length > MaxTitleLength)
            {
                return Result<Guid>.Failure("عنوان گام نمی‌تواند بیش از ۲۰۰ کاراکتر باشد.");
            }

            var description = string.IsNullOrWhiteSpace(request.Description)
                ? null
                : request.Description.Trim();

            if (description is not null && description.Length > MaxDescriptionLength)
            {
                return Result<Guid>.Failure("توضیحات گام نمی‌تواند بیش از ۱۰۰۰ کاراکتر باشد.");
            }

            var duration = string.IsNullOrWhiteSpace(request.Duration)
                ? null
                : request.Duration.Trim();

            if (duration is not null && duration.Length > MaxDurationLength)
            {
                return Result<Guid>.Failure("مدت زمان گام نمی‌تواند بیش از ۱۰۰ کاراکتر باشد.");
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

            var step = product.AddExecutionStep(title, description, duration, request.DisplayOrder);

            var audit = _auditContext.Capture();

            step.CreatorId = audit.UserId;
            step.CreateDate = audit.Timestamp;
            step.UpdaterId = audit.UserId;
            step.UpdateDate = audit.Timestamp;
            step.Ip = audit.IpAddress;

            product.UpdaterId = audit.UserId;
            product.UpdateDate = audit.Timestamp;
            product.Ip = audit.IpAddress;

            await _productRepository.UpdateAsync(product, cancellationToken);

            return Result<Guid>.Success(step.Id);
        }
    }
}

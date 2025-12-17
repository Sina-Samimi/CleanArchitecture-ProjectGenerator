using System;
using System.Threading;
using System.Threading.Tasks;
using TestAttarClone.Application.Abstractions.Messaging;
using TestAttarClone.Application.Interfaces;
using TestAttarClone.Domain.Entities.Catalog;
using TestAttarClone.SharedKernel.BaseTypes;

namespace TestAttarClone.Application.Commands.Catalog;

public sealed record CreateProductCustomRequestCommand(
    Guid ProductId,
    string FullName,
    string Phone,
    string? Email,
    string? Message,
    string? UserId) : ICommand<Guid>
{
    public sealed class Handler : ICommandHandler<CreateProductCustomRequestCommand, Guid>
    {
        private readonly IProductCustomRequestRepository _requestRepository;
        private readonly IProductRepository _productRepository;
        private readonly IAuditContext _auditContext;

        public Handler(
            IProductCustomRequestRepository requestRepository,
            IProductRepository productRepository,
            IAuditContext auditContext)
        {
            _requestRepository = requestRepository;
            _productRepository = productRepository;
            _auditContext = auditContext;
        }

        public async Task<Result<Guid>> Handle(CreateProductCustomRequestCommand request, CancellationToken cancellationToken)
        {
            if (request.ProductId == Guid.Empty)
            {
                return Result<Guid>.Failure("شناسه محصول معتبر نیست.");
            }

            if (string.IsNullOrWhiteSpace(request.FullName))
            {
                return Result<Guid>.Failure("نام و نام خانوادگی الزامی است.");
            }

            if (string.IsNullOrWhiteSpace(request.Phone))
            {
                return Result<Guid>.Failure("شماره تماس الزامی است.");
            }

            // Verify product exists and is published
            var product = await _productRepository.GetByIdAsync(request.ProductId, cancellationToken);
            if (product is null || product.IsDeleted)
            {
                return Result<Guid>.Failure("محصول مورد نظر یافت نشد.");
            }

            if (!product.IsPublished)
            {
                return Result<Guid>.Failure("محصول مورد نظر منتشر نشده است.");
            }

            if (!product.IsCustomOrder)
            {
                return Result<Guid>.Failure("این محصول حالت سفارشی ندارد.");
            }

            var customRequest = new ProductCustomRequest(
                request.ProductId,
                request.FullName,
                request.Phone,
                request.Email,
                request.Message,
                request.UserId);

            var audit = _auditContext.Capture();
            customRequest.CreatorId = audit.UserId;
            customRequest.Ip = audit.IpAddress;
            customRequest.CreateDate = audit.Timestamp;
            customRequest.UpdateDate = audit.Timestamp;
            customRequest.IsDeleted = false;

            await _requestRepository.AddAsync(customRequest, cancellationToken);

            return Result<Guid>.Success(customRequest.Id);
        }
    }
}


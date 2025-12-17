using System;
using System.Threading;
using System.Threading.Tasks;
using LogsDtoCloneTest.Application.Abstractions.Messaging;
using LogsDtoCloneTest.Application.Interfaces;
using LogsDtoCloneTest.Domain.Enums;
using LogsDtoCloneTest.SharedKernel.BaseTypes;

namespace LogsDtoCloneTest.Application.Commands.Catalog;

public sealed record RejectProductRequestCommand(
    Guid ProductRequestId,
    string? RejectionReason = null) : ICommand<bool>
{
    public sealed class Handler : ICommandHandler<RejectProductRequestCommand, bool>
{
    private readonly IProductRequestRepository _productRequestRepository;
    private readonly IAuditContext _auditContext;

    public Handler(
        IProductRequestRepository productRequestRepository,
        IAuditContext auditContext)
    {
        _productRequestRepository = productRequestRepository;
        _auditContext = auditContext;
    }

    public async Task<Result<bool>> Handle(RejectProductRequestCommand request, CancellationToken cancellationToken)
    {
        var productRequest = await _productRequestRepository.GetByIdAsync(
            request.ProductRequestId,
            cancellationToken);

        if (productRequest is null)
        {
            return Result<bool>.Failure("درخواست محصول یافت نشد.");
        }

        if (productRequest.IsDeleted)
        {
            return Result<bool>.Failure("درخواست محصول حذف شده است.");
        }

        if (productRequest.Status != ProductRequestStatus.Pending)
        {
            return Result<bool>.Failure($"درخواست محصول در وضعیت '{GetStatusText(productRequest.Status)}' است و نمی‌توان آن را رد کرد.");
        }

        var audit = _auditContext.Capture();
        if (string.IsNullOrWhiteSpace(audit.UserId))
        {
            return Result<bool>.Failure("شناسه کاربری یافت نشد.");
        }

        productRequest.Reject(audit.UserId, request.RejectionReason);
        productRequest.UpdaterId = audit.UserId;
        productRequest.UpdateDate = DateTimeOffset.UtcNow;

        await _productRequestRepository.UpdateAsync(productRequest, cancellationToken);

        return Result<bool>.Success(true);
    }

    private static string GetStatusText(ProductRequestStatus status)
    {
        return status switch
        {
            ProductRequestStatus.Pending => "در انتظار بررسی",
            ProductRequestStatus.Approved => "تایید شده",
            ProductRequestStatus.Rejected => "رد شده",
            _ => "نامشخص"
        };
    }
    }
}


using System;
using System.Threading;
using System.Threading.Tasks;
using Attar.Application.Abstractions.Messaging;
using Attar.Application.Interfaces;
using Attar.SharedKernel.BaseTypes;

namespace Attar.Application.Commands.Catalog;

public sealed record SetProductCommentApprovalCommand(Guid ProductId, Guid CommentId, bool IsApproved) : ICommand
{
    public sealed class Handler : ICommandHandler<SetProductCommentApprovalCommand>
    {
        private readonly IProductCommentRepository _productCommentRepository;
        private readonly IAuditContext _auditContext;

        public Handler(IProductCommentRepository productCommentRepository, IAuditContext auditContext)
        {
            _productCommentRepository = productCommentRepository;
            _auditContext = auditContext;
        }

        public async Task<Result> Handle(SetProductCommentApprovalCommand request, CancellationToken cancellationToken)
        {
            if (request.ProductId == Guid.Empty || request.CommentId == Guid.Empty)
            {
                return Result.Failure("اطلاعات ارسال شده برای مدیریت نظر معتبر نیست.");
            }

            var comment = await _productCommentRepository.GetByIdAsync(request.CommentId, cancellationToken);
            if (comment is null || comment.ProductId != request.ProductId)
            {
                return Result.Failure("نظر مورد نظر یافت نشد.");
            }

            var audit = _auditContext.Capture();
            comment.SetApproval(request.IsApproved, audit.UserId, audit.Timestamp);
            comment.UpdaterId = audit.UserId;
            comment.UpdateDate = audit.Timestamp;
            comment.Ip = audit.IpAddress;

            await _productCommentRepository.UpdateAsync(comment, cancellationToken);

            return Result.Success();
        }
    }
}

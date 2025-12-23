using System;
using System.Threading;
using System.Threading.Tasks;
using MobiRooz.Application.Abstractions.Messaging;
using MobiRooz.Application.Interfaces;
using MobiRooz.SharedKernel.BaseTypes;

namespace MobiRooz.Application.Commands.Blogs;

public sealed record SetBlogCommentApprovalCommand(Guid BlogId, Guid CommentId, bool IsApproved) : ICommand
{
    public sealed class Handler : ICommandHandler<SetBlogCommentApprovalCommand>
    {
        private readonly IBlogCommentRepository _blogCommentRepository;
        private readonly IAuditContext _auditContext;

        public Handler(IBlogCommentRepository blogCommentRepository, IAuditContext auditContext)
        {
            _blogCommentRepository = blogCommentRepository;
            _auditContext = auditContext;
        }

        public async Task<Result> Handle(SetBlogCommentApprovalCommand request, CancellationToken cancellationToken)
        {
            if (request.BlogId == Guid.Empty || request.CommentId == Guid.Empty)
            {
                return Result.Failure("اطلاعات ارسال شده برای مدیریت نظر معتبر نیست.");
            }

            var comment = await _blogCommentRepository.GetByIdAsync(request.CommentId, cancellationToken);
            if (comment is null || comment.BlogId != request.BlogId)
            {
                return Result.Failure("نظر مورد نظر یافت نشد.");
            }

            var audit = _auditContext.Capture();
            comment.SetApproval(request.IsApproved, audit.UserId, audit.Timestamp);
            comment.UpdaterId = audit.UserId;
            comment.UpdateDate = audit.Timestamp;
            comment.Ip = audit.IpAddress;

            await _blogCommentRepository.UpdateAsync(comment, cancellationToken);

            return Result.Success();
        }
    }
}

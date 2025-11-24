using System;
using System.Threading;
using System.Threading.Tasks;
using Arsis.Application.Abstractions.Messaging;
using Arsis.Application.Interfaces;
using Arsis.Domain.Enums;
using Arsis.SharedKernel.BaseTypes;

namespace Arsis.Application.Commands.Blogs;

public sealed record DeleteBlogCommand(Guid Id) : ICommand
{
    public sealed class Handler : ICommandHandler<DeleteBlogCommand>
    {
        private readonly IBlogRepository _blogRepository;
        private readonly IAuditContext _auditContext;

        public Handler(IBlogRepository blogRepository, IAuditContext auditContext)
        {
            _blogRepository = blogRepository;
            _auditContext = auditContext;
        }

        public async Task<Result> Handle(DeleteBlogCommand request, CancellationToken cancellationToken)
        {
            if (request.Id == Guid.Empty)
            {
                return Result.Failure("شناسه بلاگ معتبر نیست.");
            }

            var blog = await _blogRepository.GetByIdAsync(request.Id, cancellationToken);
            if (blog is null)
            {
                return Result.Failure("بلاگ مورد نظر یافت نشد.");
            }

            var audit = _auditContext.Capture();

            blog.ChangeStatus(BlogStatus.Trash, blog.PublishedAt);
            blog.IsDeleted = true;
            blog.RemoveDate = audit.Timestamp;
            blog.UpdaterId = audit.UserId;
            blog.UpdateDate = audit.Timestamp;
            blog.Ip = audit.IpAddress;

            await _blogRepository.RemoveAsync(blog, cancellationToken);

            return Result.Success();
        }
    }
}

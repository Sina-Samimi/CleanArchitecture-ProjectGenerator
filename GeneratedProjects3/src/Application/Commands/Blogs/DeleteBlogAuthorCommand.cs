using System;
using System.Threading;
using System.Threading.Tasks;
using LogTableRenameTest.Application.Abstractions.Messaging;
using LogTableRenameTest.Application.Interfaces;
using LogTableRenameTest.SharedKernel.BaseTypes;

namespace LogTableRenameTest.Application.Commands.Blogs;

public sealed record DeleteBlogAuthorCommand(Guid Id) : ICommand
{
    public sealed class Handler : ICommandHandler<DeleteBlogAuthorCommand>
    {
        private readonly IBlogAuthorRepository _authorRepository;
        private readonly IBlogRepository _blogRepository;
        private readonly IAuditContext _auditContext;

        public Handler(
            IBlogAuthorRepository authorRepository,
            IBlogRepository blogRepository,
            IAuditContext auditContext)
        {
            _authorRepository = authorRepository;
            _blogRepository = blogRepository;
            _auditContext = auditContext;
        }

        public async Task<Result> Handle(DeleteBlogAuthorCommand request, CancellationToken cancellationToken)
        {
            if (request.Id == Guid.Empty)
            {
                return Result.Failure("شناسه نویسنده معتبر نیست.");
            }

            var author = await _authorRepository.GetByIdForUpdateAsync(request.Id, cancellationToken);
            if (author is null)
            {
                return Result.Failure("نویسنده مورد نظر یافت نشد.");
            }

            if (await _blogRepository.ExistsByAuthorAsync(request.Id, cancellationToken))
            {
                return Result.Failure("ابتدا بلاگ‌های منتسب به این نویسنده را منتقل یا ویرایش کنید.");
            }

            var audit = _auditContext.Capture();

            author.IsDeleted = true;
            author.RemoveDate = audit.Timestamp;
            author.UpdaterId = audit.UserId;
            author.UpdateDate = audit.Timestamp;
            author.Ip = audit.IpAddress;

            await _authorRepository.RemoveAsync(author, cancellationToken);

            return Result.Success();
        }
    }
}

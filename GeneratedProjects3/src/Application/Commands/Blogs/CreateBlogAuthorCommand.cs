using System;
using System.Threading;
using System.Threading.Tasks;
using LogTableRenameTest.Application.Abstractions.Messaging;
using LogTableRenameTest.Application.Interfaces;
using LogTableRenameTest.Domain.Entities.Blogs;
using LogTableRenameTest.SharedKernel.BaseTypes;

namespace LogTableRenameTest.Application.Commands.Blogs;

public sealed record CreateBlogAuthorCommand(
    string DisplayName,
    string? Bio,
    string? AvatarUrl,
    bool IsActive,
    string? UserId) : ICommand<Guid>
{
    public sealed class Handler : ICommandHandler<CreateBlogAuthorCommand, Guid>
    {
        private readonly IBlogAuthorRepository _authorRepository;
        private readonly IAuditContext _auditContext;

        public Handler(IBlogAuthorRepository authorRepository, IAuditContext auditContext)
        {
            _authorRepository = authorRepository;
            _auditContext = auditContext;
        }

        public async Task<Result<Guid>> Handle(CreateBlogAuthorCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.DisplayName))
            {
                return Result<Guid>.Failure("نام نمایشی نویسنده الزامی است.");
            }

            if (await _authorRepository.ExistsByDisplayNameAsync(request.DisplayName, null, cancellationToken))
            {
                return Result<Guid>.Failure("نام نمایشی انتخاب شده تکراری است.");
            }

            if (!string.IsNullOrWhiteSpace(request.UserId) &&
                await _authorRepository.ExistsByUserAsync(request.UserId, null, cancellationToken))
            {
                return Result<Guid>.Failure("این کاربر قبلاً به عنوان نویسنده ثبت شده است.");
            }

            var author = new BlogAuthor(request.DisplayName, request.Bio, request.AvatarUrl, request.IsActive, request.UserId);

            var audit = _auditContext.Capture();

            author.CreatorId = audit.UserId;
            author.CreateDate = audit.Timestamp;
            author.UpdaterId = audit.UserId;
            author.UpdateDate = audit.Timestamp;
            author.Ip = audit.IpAddress;

            await _authorRepository.AddAsync(author, cancellationToken);

            return Result<Guid>.Success(author.Id);
        }
    }
}

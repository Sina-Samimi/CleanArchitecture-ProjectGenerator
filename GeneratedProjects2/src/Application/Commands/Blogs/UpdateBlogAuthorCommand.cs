using System;
using System.Threading;
using System.Threading.Tasks;
using LogsDtoCloneTest.Application.Abstractions.Messaging;
using LogsDtoCloneTest.Application.Interfaces;
using LogsDtoCloneTest.SharedKernel.BaseTypes;

namespace LogsDtoCloneTest.Application.Commands.Blogs;

public sealed record UpdateBlogAuthorCommand(
    Guid Id,
    string DisplayName,
    string? Bio,
    string? AvatarUrl,
    bool IsActive,
    string? UserId) : ICommand
{
    public sealed class Handler : ICommandHandler<UpdateBlogAuthorCommand>
    {
        private readonly IBlogAuthorRepository _authorRepository;
        private readonly IAuditContext _auditContext;

        public Handler(IBlogAuthorRepository authorRepository, IAuditContext auditContext)
        {
            _authorRepository = authorRepository;
            _auditContext = auditContext;
        }

        public async Task<Result> Handle(UpdateBlogAuthorCommand request, CancellationToken cancellationToken)
        {
            if (request.Id == Guid.Empty)
            {
                return Result.Failure("شناسه نویسنده معتبر نیست.");
            }

            if (string.IsNullOrWhiteSpace(request.DisplayName))
            {
                return Result.Failure("نام نمایشی نویسنده الزامی است.");
            }

            var author = await _authorRepository.GetByIdForUpdateAsync(request.Id, cancellationToken);
            if (author is null)
            {
                return Result.Failure("نویسنده مورد نظر یافت نشد.");
            }

            if (await _authorRepository.ExistsByDisplayNameAsync(request.DisplayName, request.Id, cancellationToken))
            {
                return Result.Failure("نام نمایشی انتخاب شده تکراری است.");
            }

            if (!string.IsNullOrWhiteSpace(request.UserId) &&
                await _authorRepository.ExistsByUserAsync(request.UserId, request.Id, cancellationToken))
            {
                return Result.Failure("این کاربر قبلاً به عنوان نویسنده دیگری ثبت شده است.");
            }

            author.Update(request.DisplayName, request.Bio, request.AvatarUrl, request.IsActive, request.UserId);

            var audit = _auditContext.Capture();

            author.UpdaterId = audit.UserId;
            author.UpdateDate = audit.Timestamp;
            author.Ip = audit.IpAddress;

            await _authorRepository.UpdateAsync(author, cancellationToken);

            return Result.Success();
        }
    }
}

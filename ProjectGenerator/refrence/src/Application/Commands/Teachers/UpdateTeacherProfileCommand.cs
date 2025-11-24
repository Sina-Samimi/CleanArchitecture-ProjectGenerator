using System;
using System.Threading;
using System.Threading.Tasks;
using Arsis.Application.Abstractions.Messaging;
using Arsis.Application.Interfaces;
using Arsis.SharedKernel.BaseTypes;

namespace Arsis.Application.Commands.Teachers;

public sealed record UpdateTeacherProfileCommand(
    Guid Id,
    string DisplayName,
    string? Degree,
    string? Specialty,
    string? Bio,
    string? AvatarUrl,
    string? ContactEmail,
    string? ContactPhone,
    string? UserId,
    bool IsActive) : ICommand
{
    public sealed class Handler : ICommandHandler<UpdateTeacherProfileCommand>
    {
        private readonly ITeacherProfileRepository _teacherRepository;
        private readonly IAuditContext _auditContext;

        public Handler(ITeacherProfileRepository teacherRepository, IAuditContext auditContext)
        {
            _teacherRepository = teacherRepository;
            _auditContext = auditContext;
        }

        public async Task<Result> Handle(UpdateTeacherProfileCommand request, CancellationToken cancellationToken)
        {
            var teacher = await _teacherRepository.GetByIdForUpdateAsync(request.Id, cancellationToken);
            if (teacher is null || teacher.IsDeleted)
            {
                return Result.Failure("پروفایل مدرس مورد نظر یافت نشد.");
            }

            if (string.IsNullOrWhiteSpace(request.DisplayName))
            {
                return Result.Failure("نام مدرس را وارد کنید.");
            }

            var userId = string.IsNullOrWhiteSpace(request.UserId)
                ? null
                : request.UserId.Trim();

            if (!string.IsNullOrWhiteSpace(userId))
            {
                var exists = await _teacherRepository.ExistsByUserIdAsync(userId, request.Id, cancellationToken);
                if (exists)
                {
                    return Result.Failure("این کاربر در حال حاضر به پروفایل مدرس دیگری متصل است.");
                }
            }

            teacher.UpdateDisplayName(request.DisplayName);
            teacher.UpdateAcademicInfo(request.Degree, request.Specialty, request.Bio);
            teacher.UpdateMedia(request.AvatarUrl);
            teacher.UpdateContact(request.ContactEmail, request.ContactPhone);
            teacher.ConnectToUser(userId);
            teacher.SetActive(request.IsActive);

            var audit = _auditContext.Capture();
            teacher.UpdaterId = audit.UserId;
            teacher.UpdateDate = audit.Timestamp;
            teacher.Ip = audit.IpAddress;

            await _teacherRepository.UpdateAsync(teacher, cancellationToken);

            return Result.Success();
        }
    }
}

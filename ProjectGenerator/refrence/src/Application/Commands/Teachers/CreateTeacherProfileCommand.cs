using System;
using System.Threading;
using System.Threading.Tasks;
using Arsis.Application.Abstractions.Messaging;
using Arsis.Application.Interfaces;
using Arsis.Domain.Entities.Teachers;
using Arsis.SharedKernel.BaseTypes;

namespace Arsis.Application.Commands.Teachers;

public sealed record CreateTeacherProfileCommand(
    string DisplayName,
    string? Degree,
    string? Specialty,
    string? Bio,
    string? AvatarUrl,
    string? ContactEmail,
    string? ContactPhone,
    string? UserId,
    bool IsActive) : ICommand<Guid>
{
    public sealed class Handler : ICommandHandler<CreateTeacherProfileCommand, Guid>
    {
        private readonly ITeacherProfileRepository _teacherRepository;
        private readonly IAuditContext _auditContext;

        public Handler(ITeacherProfileRepository teacherRepository, IAuditContext auditContext)
        {
            _teacherRepository = teacherRepository;
            _auditContext = auditContext;
        }

        public async Task<Result<Guid>> Handle(CreateTeacherProfileCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.DisplayName))
            {
                return Result<Guid>.Failure("نام مدرس را وارد کنید.");
            }

            var userId = string.IsNullOrWhiteSpace(request.UserId)
                ? null
                : request.UserId.Trim();

            if (!string.IsNullOrWhiteSpace(userId))
            {
                var exists = await _teacherRepository.ExistsByUserIdAsync(userId, null, cancellationToken);
                if (exists)
                {
                    return Result<Guid>.Failure("برای این کاربر قبلاً پروفایل مدرس ثبت شده است.");
                }
            }

            var teacher = new TeacherProfile(
                request.DisplayName,
                request.Degree,
                request.Specialty,
                request.Bio,
                request.AvatarUrl,
                request.ContactEmail,
                request.ContactPhone,
                userId,
                request.IsActive);

            var audit = _auditContext.Capture();

            teacher.CreatorId = audit.UserId;
            teacher.CreateDate = audit.Timestamp;
            teacher.UpdaterId = audit.UserId;
            teacher.UpdateDate = audit.Timestamp;
            teacher.Ip = audit.IpAddress;

            await _teacherRepository.AddAsync(teacher, cancellationToken);

            return Result<Guid>.Success(teacher.Id);
        }
    }
}

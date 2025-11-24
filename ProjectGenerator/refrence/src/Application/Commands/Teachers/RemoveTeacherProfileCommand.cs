using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arsis.Application.Abstractions.Messaging;
using Arsis.Application.Interfaces;
using Arsis.SharedKernel.Authorization;
using Arsis.SharedKernel.BaseTypes;
using Microsoft.AspNetCore.Identity;

namespace Arsis.Application.Commands.Teachers;

public sealed record RemoveTeacherProfileCommand(Guid TeacherId) : ICommand
{
    public sealed class Handler : ICommandHandler<RemoveTeacherProfileCommand>
    {
        private readonly ITeacherProfileRepository _teacherRepository;
        private readonly IAuditContext _auditContext;
        private readonly UserManager<Domain.Entities.ApplicationUser> _userManager;

        public Handler(
            ITeacherProfileRepository teacherRepository,
            IAuditContext auditContext,
            UserManager<Domain.Entities.ApplicationUser> userManager)
        {
            _teacherRepository = teacherRepository;
            _auditContext = auditContext;
            _userManager = userManager;
        }

        public async Task<Result> Handle(RemoveTeacherProfileCommand request, CancellationToken cancellationToken)
        {
            var teacher = await _teacherRepository.GetByIdForUpdateAsync(request.TeacherId, cancellationToken);
            if (teacher is null || teacher.IsDeleted)
            {
                return Result.Failure("پروفایل مدرس مورد نظر یافت نشد.");
            }

            var audit = _auditContext.Capture();

            if (!string.IsNullOrWhiteSpace(teacher.UserId))
            {
                var user = await _userManager.FindByIdAsync(teacher.UserId);
                if (user is not null && !user.IsDeleted)
                {
                    var removeRoleResult = await _userManager.RemoveFromRoleAsync(user, RoleNames.Teacher);
                    if (!removeRoleResult.Succeeded)
                    {
                        return Result.Failure(string.Join("; ", removeRoleResult.Errors.Select(error => error.Description)));
                    }

                    user.LastModifiedOn = DateTimeOffset.UtcNow;

                    var updateResult = await _userManager.UpdateAsync(user);
                    if (!updateResult.Succeeded)
                    {
                        return Result.Failure(string.Join("; ", updateResult.Errors.Select(error => error.Description)));
                    }

                    var stampResult = await _userManager.UpdateSecurityStampAsync(user);
                    if (!stampResult.Succeeded)
                    {
                        return Result.Failure(string.Join("; ", stampResult.Errors.Select(error => error.Description)));
                    }
                }
            }

            teacher.SetActive(false);
            teacher.ConnectToUser(null);
            teacher.IsDeleted = true;
            teacher.RemoveDate = audit.Timestamp;
            teacher.UpdaterId = audit.UserId;
            teacher.UpdateDate = audit.Timestamp;
            teacher.Ip = audit.IpAddress;

            await _teacherRepository.UpdateAsync(teacher, cancellationToken);

            return Result.Success();
        }
    }
}

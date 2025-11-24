using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arsis.Application.Abstractions.Messaging;
using Arsis.Application.Interfaces;
using Arsis.SharedKernel.BaseTypes;
using Microsoft.AspNetCore.Identity;

namespace Arsis.Application.Commands.Teachers;

public sealed record DeactivateTeacherCommand(Guid TeacherId, string? Reason = null) : ICommand
{
    public sealed class Handler : ICommandHandler<DeactivateTeacherCommand>
    {
        private const string DefaultReason = "حساب مدرس به صورت موقت غیرفعال شده است. لطفاً با پشتیبانی تماس بگیرید.";

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

        public async Task<Result> Handle(DeactivateTeacherCommand request, CancellationToken cancellationToken)
        {
            var teacher = await _teacherRepository.GetByIdForUpdateAsync(request.TeacherId, cancellationToken);
            if (teacher is null || teacher.IsDeleted)
            {
                return Result.Failure("پروفایل مدرس مورد نظر یافت نشد.");
            }

            teacher.SetActive(false);

            var audit = _auditContext.Capture();
            teacher.UpdaterId = audit.UserId;
            teacher.UpdateDate = audit.Timestamp;
            teacher.Ip = audit.IpAddress;

            if (!string.IsNullOrWhiteSpace(teacher.UserId))
            {
                var user = await _userManager.FindByIdAsync(teacher.UserId);
                if (user is not null && !user.IsDeleted)
                {
                    user.IsActive = false;
                    user.DeactivatedOn = DateTimeOffset.UtcNow;
                    user.DeactivationReason = string.IsNullOrWhiteSpace(request.Reason)
                        ? DefaultReason
                        : request.Reason!.Trim();
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

            await _teacherRepository.UpdateAsync(teacher, cancellationToken);

            return Result.Success();
        }
    }
}

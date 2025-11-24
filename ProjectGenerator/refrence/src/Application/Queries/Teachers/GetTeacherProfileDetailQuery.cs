using System;
using System.Threading;
using System.Threading.Tasks;
using Arsis.Application.Abstractions.Messaging;
using Arsis.Application.DTOs.Teachers;
using Arsis.Application.Interfaces;
using Arsis.SharedKernel.BaseTypes;

namespace Arsis.Application.Queries.Teachers;

public sealed record GetTeacherProfileDetailQuery(Guid Id) : IQuery<TeacherProfileDetailDto>
{
    public sealed class Handler : IQueryHandler<GetTeacherProfileDetailQuery, TeacherProfileDetailDto>
    {
        private readonly ITeacherProfileRepository _teacherRepository;

        public Handler(ITeacherProfileRepository teacherRepository)
        {
            _teacherRepository = teacherRepository;
        }

        public async Task<Result<TeacherProfileDetailDto>> Handle(GetTeacherProfileDetailQuery request, CancellationToken cancellationToken)
        {
            var teacher = await _teacherRepository.GetByIdAsync(request.Id, cancellationToken);
            if (teacher is null || teacher.IsDeleted)
            {
                return Result<TeacherProfileDetailDto>.Failure("پروفایل مدرس یافت نشد.");
            }

            var dto = new TeacherProfileDetailDto(
                teacher.Id,
                teacher.DisplayName,
                teacher.Degree,
                teacher.Specialty,
                teacher.Bio,
                teacher.AvatarUrl,
                teacher.ContactEmail,
                teacher.ContactPhone,
                teacher.UserId,
                teacher.IsActive,
                teacher.CreateDate,
                teacher.UpdateDate);

            return Result<TeacherProfileDetailDto>.Success(dto);
        }
    }
}

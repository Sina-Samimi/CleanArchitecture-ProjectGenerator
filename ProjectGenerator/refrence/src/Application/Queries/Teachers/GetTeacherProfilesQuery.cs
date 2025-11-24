using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arsis.Application.Abstractions.Messaging;
using Arsis.Application.DTOs.Teachers;
using Arsis.Application.Interfaces;
using Arsis.SharedKernel.BaseTypes;

namespace Arsis.Application.Queries.Teachers;

public sealed record GetTeacherProfilesQuery : IQuery<TeacherProfileListResultDto>
{
    public sealed class Handler : IQueryHandler<GetTeacherProfilesQuery, TeacherProfileListResultDto>
    {
        private readonly ITeacherProfileRepository _teacherRepository;

        public Handler(ITeacherProfileRepository teacherRepository)
        {
            _teacherRepository = teacherRepository;
        }

        public async Task<Result<TeacherProfileListResultDto>> Handle(GetTeacherProfilesQuery request, CancellationToken cancellationToken)
        {
            var teachers = await _teacherRepository.GetAllAsync(cancellationToken);

            var items = teachers
                .Where(teacher => !teacher.IsDeleted)
                .OrderByDescending(teacher => teacher.UpdateDate)
                .Select(teacher => new TeacherProfileListItemDto(
                    teacher.Id,
                    teacher.DisplayName,
                    teacher.Degree,
                    teacher.Specialty,
                    teacher.Bio,
                    teacher.ContactEmail,
                    teacher.ContactPhone,
                    teacher.UserId,
                    teacher.IsActive,
                    teacher.CreateDate,
                    teacher.UpdateDate))
                .ToArray();

            var activeCount = items.Count(item => item.IsActive);
            var inactiveCount = items.Length - activeCount;

            return Result<TeacherProfileListResultDto>.Success(new TeacherProfileListResultDto(items, activeCount, inactiveCount));
        }
    }
}

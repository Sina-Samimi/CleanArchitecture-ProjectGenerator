using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arsis.Application.Abstractions.Messaging;
using Arsis.Application.DTOs.Teachers;
using Arsis.Application.Interfaces;
using Arsis.SharedKernel.BaseTypes;

namespace Arsis.Application.Queries.Teachers;

public sealed record GetTeacherLookupsQuery : IQuery<IReadOnlyCollection<TeacherLookupDto>>
{
    public sealed class Handler : IQueryHandler<GetTeacherLookupsQuery, IReadOnlyCollection<TeacherLookupDto>>
    {
        private readonly ITeacherProfileRepository _teacherRepository;

        public Handler(ITeacherProfileRepository teacherRepository)
        {
            _teacherRepository = teacherRepository;
        }

        public async Task<Result<IReadOnlyCollection<TeacherLookupDto>>> Handle(GetTeacherLookupsQuery request, CancellationToken cancellationToken)
        {
            var teachers = await _teacherRepository.GetAllAsync(cancellationToken);

            var lookups = teachers
                .Where(teacher => !string.IsNullOrWhiteSpace(teacher.UserId))
                .OrderByDescending(teacher => teacher.IsActive)
                .ThenBy(teacher => teacher.DisplayName)
                .Select(teacher => new TeacherLookupDto(
                    teacher.Id,
                    teacher.DisplayName,
                    teacher.Degree,
                    teacher.UserId,
                    teacher.IsActive))
                .ToArray();

            return Result<IReadOnlyCollection<TeacherLookupDto>>.Success(lookups);
        }
    }
}

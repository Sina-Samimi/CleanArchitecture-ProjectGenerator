using Arsis.Application.Abstractions.Messaging;
using Arsis.Application.DTOs;
using Arsis.Application.Interfaces;
using Arsis.SharedKernel.BaseTypes;

namespace Arsis.Application.Commands.Reports;

public sealed record GenerateReportCommand(Guid UserId, IReadOnlyCollection<TalentScoreDto> Talents) : ICommand<ReportDocumentDto>
{
    public sealed class Handler : ICommandHandler<GenerateReportCommand, ReportDocumentDto>
    {
        private readonly IReportGenerator _reportGenerator;

        public Handler(IReportGenerator reportGenerator)
        {
            _reportGenerator = reportGenerator;
        }

        public async Task<Result<ReportDocumentDto>> Handle(GenerateReportCommand request, CancellationToken cancellationToken)
        {
            if (request.UserId == Guid.Empty)
            {
                return Result<ReportDocumentDto>.Failure("User id cannot be empty.");
            }

            if (request.Talents is null || request.Talents.Count == 0)
            {
                return Result<ReportDocumentDto>.Failure("At least one talent is required to generate the report.");
            }

            var document = await _reportGenerator.GenerateAsync(
                new ReportRequestDto(request.UserId, request.Talents),
                cancellationToken);

            return Result<ReportDocumentDto>.Success(document);
        }
    }
}

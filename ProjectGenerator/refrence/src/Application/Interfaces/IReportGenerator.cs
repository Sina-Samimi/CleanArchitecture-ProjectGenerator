using Arsis.Application.DTOs;

namespace Arsis.Application.Interfaces;

public interface IReportGenerator
{
    Task<ReportDocumentDto> GenerateAsync(ReportRequestDto request, CancellationToken cancellationToken);
}

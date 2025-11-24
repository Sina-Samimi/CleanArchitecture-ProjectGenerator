using System.Text;
using Arsis.Application.DTOs;
using Arsis.Application.Interfaces;

namespace Arsis.Infrastructure.Services;

public sealed class ReportGenerator : IReportGenerator
{
    public Task<ReportDocumentDto> GenerateAsync(ReportRequestDto request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var builder = new StringBuilder();
        builder.AppendLine($"Talent Report for User {request.UserId}");
        builder.AppendLine(new string('-', 40));

        foreach (var talent in request.TopTalents.OrderByDescending(t => t.Score))
        {
            builder.AppendLine($"{talent.TalentName}: {talent.Score:0.##}");
        }

        var content = Encoding.UTF8.GetBytes(builder.ToString());
        var document = new ReportDocumentDto(
            $"talent-report-{request.UserId}.txt",
            "text/plain",
            content);

        return Task.FromResult(document);
    }
}

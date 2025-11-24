namespace Arsis.Application.DTOs;

public sealed record ReportRequestDto(Guid UserId, IReadOnlyCollection<TalentScoreDto> TopTalents);

public sealed record ReportDocumentDto(string FileName, string ContentType, byte[] Content);

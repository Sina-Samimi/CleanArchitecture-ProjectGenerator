namespace Arsis.Application.DTOs;

public sealed record SubmitTestResultDto(Guid UserId, IReadOnlyCollection<TalentScoreDto> TopTalents, ReportDocumentDto Report);

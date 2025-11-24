namespace Arsis.Application.DTOs.OrganizationAnalysis;

public record UserTestResultDto(
    string UserId,
    string Name,
    string LastName,
    DateTime TestDateTime,
    Guid AttemptId,
    Guid TestId,
    string JobGroup,
    double Result);

namespace Arsis.Application.DTOs.OrganizationAnalysis;

public record WeakUserDto(
    string UserId,
    string Name,
    string LastName,
    Guid AttemptId,
    Guid TestId,
    DateTime LastTestDateTime);

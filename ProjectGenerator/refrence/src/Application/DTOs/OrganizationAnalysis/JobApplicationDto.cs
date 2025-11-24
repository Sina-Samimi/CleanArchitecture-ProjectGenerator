namespace Arsis.Application.DTOs.OrganizationAnalysis;

public record JobApplicationDto(
    long JobId,
    string Title,
    string Description,
    DateTime PostedDate,
    string OrganizationName,
    string OrganizationLogo,
    bool ApplicationStatus);

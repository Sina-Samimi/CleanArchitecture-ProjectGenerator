using Arsis.Application.DTOs.OrganizationAnalysis;

namespace Arsis.Application.Interfaces;

public interface IOrganizationAnalysisService
{
    Task<IReadOnlyList<WeakUserDto>> GetWeakUsersAsync(Guid organizationId, string weaknessType, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OrganizationWeaknessDto>> GetOrganizationWeaknessesAsync(Guid organizationId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserTestResultDto>> GetUserTestResultsAsync(Guid organizationId, string jobGroup, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<JobApplicationDto>> GetJobOffersAsync(string userId, CancellationToken cancellationToken = default);
}

using Arsis.Domain.Entities;

namespace Arsis.Application.Interfaces;

public interface ITestSubmissionRepository
{
    Task SaveResponsesAsync(IEnumerable<UserResponse> responses, CancellationToken cancellationToken);
}

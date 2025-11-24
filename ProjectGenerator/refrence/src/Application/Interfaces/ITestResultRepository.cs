using Arsis.Domain.Entities.Tests;

namespace Arsis.Application.Interfaces;

public interface ITestResultRepository
{
    Task<TestResult?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<TestResult>> GetByAttemptIdAsync(Guid attemptId, CancellationToken cancellationToken = default);
    Task<List<TestResult>> GetUserResultsAsync(string userId, CancellationToken cancellationToken = default);
    Task AddAsync(TestResult result, CancellationToken cancellationToken = default);
    Task AddRangeAsync(List<TestResult> results, CancellationToken cancellationToken = default);
    Task UpdateAsync(TestResult result, CancellationToken cancellationToken = default);
    Task DeleteByAttemptIdAsync(Guid attemptId, CancellationToken cancellationToken = default);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

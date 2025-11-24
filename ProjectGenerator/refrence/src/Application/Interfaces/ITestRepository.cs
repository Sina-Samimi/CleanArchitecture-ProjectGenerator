using Arsis.Domain.Entities.Tests;
using Arsis.Domain.Enums;

namespace Arsis.Application.Interfaces;

public interface ITestRepository
{
    Task<Test?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Test?> GetByIdWithQuestionsAsync(
        Guid id,
        CancellationToken cancellationToken = default,
        bool asTracking = true);
    Task<List<Test>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<List<Test>> GetByTypeAsync(TestType type, CancellationToken cancellationToken = default);
    Task<List<Test>> GetPublishedAsync(CancellationToken cancellationToken = default);
    Task<(List<Test> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        TestType? type = null,
        TestStatus? status = null,
        string? searchTerm = null,
        CancellationToken cancellationToken = default);
    Task AddAsync(Test test, CancellationToken cancellationToken = default);
    void AddQuestionGraph(TestQuestion question);
    Task UpdateAsync(Test test, CancellationToken cancellationToken = default);
    Task DeleteAsync(Test test, CancellationToken cancellationToken = default);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

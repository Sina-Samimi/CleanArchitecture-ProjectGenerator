using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Arsis.Application.DTOs.Tests;
using Arsis.Domain.Entities.Tests;
using Arsis.Domain.Enums;

namespace Arsis.Application.Interfaces;

public interface IUserTestAttemptRepository
{
    Task<UserTestAttempt?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<UserTestAttempt?> GetByIdWithAnswersAsync(
        Guid id,
        CancellationToken cancellationToken = default,
        bool includeDetails = false,
        bool asTracking = false);
    Task<List<UserTestAttempt>> GetUserAttemptsAsync(string userId, CancellationToken cancellationToken = default);
    Task<List<UserTestAttempt>> GetUserAttemptsForTestAsync(
        string userId,
        Guid testId,
        CancellationToken cancellationToken = default);
    Task<UserTestAttempt?> GetLatestAttemptAsync(
        string userId,
        Guid testId,
        CancellationToken cancellationToken = default);
    Task<int> GetUserAttemptCountAsync(
        string userId,
        Guid testId,
        TestAttemptStatus? status = null,
        CancellationToken cancellationToken = default);
    Task<UserTestAttempt?> GetActiveAttemptAsync(
        string userId,
        Guid testId,
        CancellationToken cancellationToken = default);
    Task<UserTestAttempt?> GetByInvoiceIdAsync(
        Guid invoiceId,
        string? userId = default,
        CancellationToken cancellationToken = default);
    Task<(List<UserTestAttempt> Items, int TotalCount, TestAttemptStatisticsDto Statistics)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        Guid? testId = null,
        string? userId = null,
        TestAttemptStatus? status = null,
        DateTimeOffset? startedFrom = null,
        DateTimeOffset? startedTo = null,
        string? searchTerm = null,
        CancellationToken cancellationToken = default);
    Task AddAsync(UserTestAttempt attempt, CancellationToken cancellationToken = default);
    Task UpdateAsync(UserTestAttempt attempt, CancellationToken cancellationToken = default);
    Task DeleteAsync(UserTestAttempt attempt, CancellationToken cancellationToken = default);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

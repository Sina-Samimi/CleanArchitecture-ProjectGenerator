using System.Collections.Generic;

namespace LogsDtoCloneTest.Application.DTOs.Tests;

public sealed record class PagedUserTestAttemptsDto
{
    public List<UserTestAttemptDto> Items { get; init; } = new();
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalPages { get; init; }
    public TestAttemptStatisticsDto Statistics { get; init; } = TestAttemptStatisticsDto.Empty;
}

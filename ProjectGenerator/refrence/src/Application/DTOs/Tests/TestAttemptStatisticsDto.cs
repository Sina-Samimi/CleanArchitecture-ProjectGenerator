using System;

namespace Arsis.Application.DTOs.Tests;

public sealed record class TestAttemptStatisticsDto
{
    public int TotalAttempts { get; init; }
    public int CompletedAttempts { get; init; }
    public int InProgressAttempts { get; init; }
    public int CancelledAttempts { get; init; }
    public int ExpiredAttempts { get; init; }
    public int UniqueParticipants { get; init; }
    public decimal? AverageScore { get; init; }
    public double? AverageCompletionMinutes { get; init; }
    public DateTimeOffset? FirstAttemptAt { get; init; }
    public DateTimeOffset? LastAttemptAt { get; init; }

    public static TestAttemptStatisticsDto Empty { get; } = new();
}

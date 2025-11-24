using Arsis.Application.DTOs.Tests;

namespace EndPoint.WebSite.Areas.Admin.Models.Tests;

public sealed class TestAttemptDetailViewModel
{
    public UserTestAttemptDetailDto Attempt { get; init; } = null!;
    public string TestTypeTitle { get; init; } = string.Empty;
    public string StatusTitle { get; init; } = string.Empty;
    public string? UserFullName { get; init; }
    public string? UserEmail { get; init; }
    public string? UserPhoneNumber { get; init; }
}

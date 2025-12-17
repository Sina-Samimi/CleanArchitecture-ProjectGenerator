using System;

namespace LogTableRenameTest.WebSite.Services;

public enum PhoneVerificationError
{
    None,
    NotFound,
    Expired,
    Incorrect
}

public sealed record PhoneVerificationCode(string PhoneNumber, string Code, DateTimeOffset ExpiresAt);

public sealed record PhoneVerificationValidationResult(bool Succeeded, PhoneVerificationError Error, DateTimeOffset? ExpiresAt);

public sealed record PhoneVerificationState(DateTimeOffset GeneratedAt, DateTimeOffset ExpiresAt);

using System;

namespace Arsis.Application.Queries.Identity.GetUsers;

public sealed record UserFilterCriteria(
    bool IncludeDeactivated,
    bool IncludeDeleted,
    string? FullName,
    string? PhoneNumber,
    string? Role,
    UserStatusFilter Status,
    DateTimeOffset? RegisteredFrom,
    DateTimeOffset? RegisteredTo);

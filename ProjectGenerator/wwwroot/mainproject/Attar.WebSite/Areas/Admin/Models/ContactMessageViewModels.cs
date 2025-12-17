using System;
using System.Collections.Generic;

namespace Attar.WebSite.Areas.Admin.Models;

public sealed class ContactMessagesListViewModel
{
    public IReadOnlyCollection<ContactMessageViewModel> Messages { get; init; }
        = Array.Empty<ContactMessageViewModel>();

    public int TotalCount { get; init; }

    public int PageNumber { get; init; }

    public int PageSize { get; init; }

    public bool UnreadOnly { get; init; }
}

public sealed class ContactMessageViewModel
{
    public Guid Id { get; init; }

    public string? UserId { get; init; }

    public string FullName { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public string Phone { get; init; } = string.Empty;

    public string Subject { get; init; } = string.Empty;

    public string Message { get; init; } = string.Empty;

    public bool IsRead { get; init; }

    public DateTimeOffset? ReadAt { get; init; }

    public string? ReadByUserId { get; init; }

    public string? AdminReply { get; init; }

    public DateTimeOffset? RepliedAt { get; init; }

    public string? RepliedByUserId { get; init; }

    public DateTimeOffset CreateDate { get; init; }
}


using System;
using System.Collections.Generic;
using Attar.Domain.Enums;

namespace Attar.WebSite.Areas.User.Models;

public sealed class TicketListViewModel
{
    public IReadOnlyCollection<TicketViewModel> Tickets { get; init; }
        = Array.Empty<TicketViewModel>();

    public int TotalCount { get; init; }

    public int PageNumber { get; init; }

    public int PageSize { get; init; }

    public int TotalPages { get; init; }

    public TicketStatusCounts StatusCounts { get; init; } = new();
}

public sealed class TicketStatusCounts
{
    public int OpenCount { get; init; }
    public int InProgressCount { get; init; }
    public int AnsweredCount { get; init; }
    public int ClosedCount { get; init; }
}

public sealed class TicketViewModel
{
    public Guid Id { get; init; }

    public string Subject { get; init; } = string.Empty;

    public string Message { get; init; } = string.Empty;

    public string? Department { get; init; }

    public TicketStatus Status { get; init; }

    public DateTimeOffset CreateDate { get; init; }

    public DateTimeOffset? LastReplyDate { get; init; }

    public bool HasUnreadReplies { get; init; }

    public int RepliesCount { get; init; }

    public string TicketNumber { get; init; } = string.Empty;
}

public sealed class TicketDetailViewModel
{
    public Guid Id { get; init; }

    public string Subject { get; init; } = string.Empty;

    public string Message { get; init; } = string.Empty;

    public string? Department { get; init; }

    public string? AttachmentPath { get; init; }

    public TicketStatus Status { get; init; }

    public DateTimeOffset CreateDate { get; init; }

    public DateTimeOffset? LastReplyDate { get; init; }

    public bool HasUnreadReplies { get; init; }

    public IReadOnlyCollection<TicketReplyViewModel> Replies { get; init; }
        = Array.Empty<TicketReplyViewModel>();

    public CreateTicketViewModel CreateForm { get; init; } = new();

    public CreateTicketReplyViewModel ReplyForm { get; init; } = new();

    public string? CurrentUserName { get; set; }
}

public sealed class TicketReplyViewModel
{
    public Guid Id { get; init; }

    public Guid TicketId { get; init; }

    public string Message { get; init; } = string.Empty;

    public bool IsFromAdmin { get; init; }

    public string? RepliedByName { get; init; }

    public DateTimeOffset CreateDate { get; init; }
}

public sealed class CreateTicketViewModel
{
    public string Subject { get; init; } = string.Empty;

    public string Message { get; init; } = string.Empty;

    public string? Department { get; init; }

    public string? SelectedDepartment { get; init; }
}

public sealed class CreateTicketReplyViewModel
{
    public Guid TicketId { get; init; }

    public string Message { get; init; } = string.Empty;
}

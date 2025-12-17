using System;
using System.Collections.Generic;
using LogTableRenameTest.Application.DTOs;
using LogTableRenameTest.Domain.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace LogTableRenameTest.WebSite.Areas.Admin.Models;

public sealed class TicketListViewModel
{
    public IReadOnlyCollection<TicketViewModel> Tickets { get; init; }
        = Array.Empty<TicketViewModel>();

    public int TotalCount { get; init; }

    public int PageNumber { get; init; }

    public int PageSize { get; init; }

    public int TotalPages { get; init; }

    public TicketFilterViewModel Filter { get; init; } = new();
}

public sealed class TicketFilterViewModel
{
    public string? UserId { get; init; }

    public TicketStatus? Status { get; init; }

    public string? AssignedToId { get; init; }
}

public sealed class TicketViewModel
{
    public Guid Id { get; init; }

    public string UserId { get; init; } = string.Empty;

    public string UserName { get; init; } = string.Empty;

    public string UserFullName { get; init; } = string.Empty;

    public string? UserPhoneNumber { get; init; }

    public string Subject { get; init; } = string.Empty;

    public string Message { get; init; } = string.Empty;

    public string? Department { get; init; }
    public string? AttachmentPath { get; set; }

    public TicketStatus Status { get; init; }

    public string? AssignedToId { get; init; }

    public string? AssignedToName { get; init; }

    public DateTimeOffset CreateDate { get; init; }

    public DateTimeOffset? LastReplyDate { get; init; }

    public bool HasUnreadReplies { get; init; }

    public int RepliesCount { get; init; }
}

public sealed class TicketDetailViewModel
{
    public Guid Id { get; init; }

    public string UserId { get; init; } = string.Empty;

    public string UserName { get; init; } = string.Empty;

    public string UserFullName { get; init; } = string.Empty;

    public string? UserPhoneNumber { get; init; }

    public string Subject { get; init; } = string.Empty;

    public string Message { get; init; } = string.Empty;

    public string? Department { get; init; }

    public string? AttachmentPath { get; init; }

    public TicketStatus Status { get; init; }

    public string? AssignedToId { get; init; }

    public string? AssignedToName { get; init; }

    public DateTimeOffset CreateDate { get; init; }

    public DateTimeOffset? LastReplyDate { get; init; }

    public bool HasUnreadReplies { get; init; }

    public IReadOnlyCollection<TicketReplyViewModel> Replies { get; init; }
        = Array.Empty<TicketReplyViewModel>();

    public CreateTicketReplyViewModel ReplyForm { get; init; } = new();

    public string? CurrentUserName { get; set; }

    public IReadOnlyCollection<SelectListItem> AdminUserOptions { get; init; } = Array.Empty<SelectListItem>();
}

public sealed class TicketReplyViewModel
{
    public Guid Id { get; init; }

    public Guid TicketId { get; init; }

    public string Message { get; init; } = string.Empty;

    public bool IsFromAdmin { get; init; }

    public string? RepliedById { get; init; }

    public string? RepliedByName { get; init; }

    public DateTimeOffset CreateDate { get; init; }
}

public sealed class CreateTicketReplyViewModel
{
    public Guid TicketId { get; init; }

    public string Message { get; init; } = string.Empty;

    public string? CurrentUserName { get; init; }
}

using System;
using System.Collections.Generic;
using Attar.Domain.Enums;

namespace Attar.Application.DTOs.Tickets;

public sealed record TicketDto(
    Guid Id,
    string UserId,
    string UserName,
    string UserFullName,
    string? UserPhoneNumber,
    string Subject,
    string Message,
    string? Department,
    string? AttachmentPath,
    TicketStatus Status,
    string? AssignedToId,
    string? AssignedToName,
    DateTimeOffset CreateDate,
    DateTimeOffset? LastReplyDate,
    bool HasUnreadReplies,
    int RepliesCount);

public sealed record TicketReplyDto(
    Guid Id,
    Guid TicketId,
    string Message,
    bool IsFromAdmin,
    string? RepliedById,
    string? RepliedByName,
    DateTimeOffset CreateDate);

public sealed record TicketDetailDto(
    Guid Id,
    string UserId,
    string UserName,
    string UserFullName,
    string? UserPhoneNumber,
    string Subject,
    string Message,
    string? Department,
    string? AttachmentPath,
    TicketStatus Status,
    string? AssignedToId,
    string? AssignedToName,
    DateTimeOffset CreateDate,
    DateTimeOffset? LastReplyDate,
    bool HasUnreadReplies,
    IReadOnlyCollection<TicketReplyDto> Replies);

public sealed record TicketListResultDto(
    IReadOnlyCollection<TicketDto> Tickets,
    int TotalCount,
    int PageNumber,
    int PageSize,
    int TotalPages);

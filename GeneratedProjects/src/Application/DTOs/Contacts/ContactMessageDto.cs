using System;

namespace TestAttarClone.Application.DTOs.Contacts;

public sealed record ContactMessageDto(
    Guid Id,
    string? UserId,
    string FullName,
    string Email,
    string Phone,
    string Subject,
    string Message,
    bool IsRead,
    DateTimeOffset? ReadAt,
    string? ReadByUserId,
    string? AdminReply,
    DateTimeOffset? RepliedAt,
    string? RepliedByUserId,
    DateTimeOffset CreateDate);

public sealed record ContactMessagesListDto(
    IReadOnlyList<ContactMessageDto> Messages,
    int TotalCount,
    int PageNumber,
    int PageSize);


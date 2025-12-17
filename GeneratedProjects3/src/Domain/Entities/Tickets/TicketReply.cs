using System;
using System.Diagnostics.CodeAnalysis;
using LogTableRenameTest.Domain.Base;
using LogTableRenameTest.Domain.Entities;

namespace LogTableRenameTest.Domain.Entities.Tickets;

public sealed class TicketReply : Entity
{
    [SetsRequiredMembers]
    private TicketReply()
    {
        Message = string.Empty;
    }

    [SetsRequiredMembers]
    public TicketReply(
        Guid ticketId,
        string message,
        bool isFromAdmin,
        string? repliedById = null)
    {
        if (ticketId == Guid.Empty)
        {
            throw new ArgumentException("Ticket ID cannot be empty", nameof(ticketId));
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("Message cannot be empty", nameof(message));
        }

        TicketId = ticketId;
        Message = message.Trim();
        IsFromAdmin = isFromAdmin;
        RepliedById = string.IsNullOrWhiteSpace(repliedById) ? null : repliedById.Trim();
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public Guid TicketId { get; private set; }

    public Ticket Ticket { get; private set; } = null!;

    public string Message { get; private set; } = string.Empty;

    public bool IsFromAdmin { get; private set; }

    public string? RepliedById { get; private set; }

    public ApplicationUser? RepliedBy { get; private set; }
}

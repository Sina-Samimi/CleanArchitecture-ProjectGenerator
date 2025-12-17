using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Attar.Domain.Entities.Tickets;
using Attar.Domain.Enums;

namespace Attar.Application.Interfaces;

public interface ITicketRepository
{
    Task<Ticket?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<Ticket?> GetByIdWithRepliesAsync(Guid id, CancellationToken cancellationToken);

    Task AddAsync(Ticket ticket, CancellationToken cancellationToken);

    Task UpdateAsync(Ticket ticket, CancellationToken cancellationToken);

    Task AddReplyAsync(TicketReply reply, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<Ticket>> GetByUserIdAsync(string userId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<Ticket>> GetAllAsync(
        string? userId,
        TicketStatus? status,
        string? assignedToId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken);

    Task<int> GetCountAsync(
        string? userId,
        TicketStatus? status,
        string? assignedToId,
        CancellationToken cancellationToken);

    Task<int> GetNewTicketsCountAsync(CancellationToken cancellationToken);

    Task<int> GetUnreadRepliesCountForUserAsync(string userId, CancellationToken cancellationToken);
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LogsDtoCloneTest.Application.Interfaces;
using LogsDtoCloneTest.Domain.Entities.Tickets;
using LogsDtoCloneTest.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LogsDtoCloneTest.Infrastructure.Persistence.Repositories;

public sealed class TicketRepository : ITicketRepository
{
    private readonly AppDbContext _dbContext;

    public TicketRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Ticket?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
        {
            return null;
        }

        return await _dbContext.Tickets
            .Include(t => t.User)
            .Include(t => t.AssignedTo)
            .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted, cancellationToken);
    }

    public async Task<Ticket?> GetByIdWithRepliesAsync(Guid id, CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
        {
            return null;
        }

        return await _dbContext.Tickets
            .Include(t => t.User)
            .Include(t => t.AssignedTo)
            .Include(t => t.Replies)
                .ThenInclude(r => r.RepliedBy)
            .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted, cancellationToken);
    }

    public async Task AddAsync(Ticket ticket, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(ticket);

        await _dbContext.Tickets.AddAsync(ticket, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Ticket ticket, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(ticket);

        _dbContext.Tickets.Update(ticket);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task AddReplyAsync(TicketReply reply, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(reply);

        await _dbContext.TicketReplies.AddAsync(reply, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<Ticket>> GetByUserIdAsync(string userId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Array.Empty<Ticket>();
        }

        return await _dbContext.Tickets
            .AsNoTracking()
            .Include(t => t.User)
            .Include(t => t.AssignedTo)
            .Where(t => !t.IsDeleted && t.UserId == userId)
            .OrderByDescending(t => t.CreateDate)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<Ticket>> GetAllAsync(
        string? userId,
        TicketStatus? status,
        string? assignedToId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        if (pageNumber < 1)
        {
            pageNumber = 1;
        }

        if (pageSize < 1)
        {
            pageSize = 10;
        }

        var query = _dbContext.Tickets
            .AsNoTracking()
            .Include(t => t.User)
            .Include(t => t.AssignedTo)
            .Where(t => !t.IsDeleted);

        if (!string.IsNullOrWhiteSpace(userId))
        {
            query = query.Where(t => t.UserId == userId);
        }

        if (status.HasValue)
        {
            query = query.Where(t => t.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(assignedToId))
        {
            query = query.Where(t => t.AssignedToId == assignedToId);
        }

        return await query
            .OrderByDescending(t => t.CreateDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<int> GetCountAsync(
        string? userId,
        TicketStatus? status,
        string? assignedToId,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.Tickets
            .AsNoTracking()
            .Where(t => !t.IsDeleted);

        if (!string.IsNullOrWhiteSpace(userId))
        {
            query = query.Where(t => t.UserId == userId);
        }

        if (status.HasValue)
        {
            query = query.Where(t => t.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(assignedToId))
        {
            query = query.Where(t => t.AssignedToId == assignedToId);
        }

        return await query.CountAsync(cancellationToken);
    }

    public async Task<int> GetNewTicketsCountAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.Tickets
            .AsNoTracking()
            .Where(t => !t.IsDeleted && t.Status == TicketStatus.Pending)
            .CountAsync(cancellationToken);
    }

    public async Task<int> GetUnreadRepliesCountForUserAsync(string userId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return 0;
        }

        return await _dbContext.Tickets
            .AsNoTracking()
            .Where(t => !t.IsDeleted && t.UserId == userId && t.HasUnreadReplies)
            .CountAsync(cancellationToken);
    }
}

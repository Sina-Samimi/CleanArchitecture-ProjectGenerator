using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TestAttarClone.Application.Interfaces;
using TestAttarClone.Domain.Entities.Contacts;
using Microsoft.EntityFrameworkCore;

namespace TestAttarClone.Infrastructure.Persistence.Repositories;

public sealed class ContactMessageRepository : IContactMessageRepository
{
    private readonly AppDbContext _dbContext;

    public ContactMessageRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ContactMessage?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
        {
            return null;
        }

        return await _dbContext.ContactMessages
            .FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted, cancellationToken);
    }

    public async Task AddAsync(ContactMessage message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);

        await _dbContext.ContactMessages.AddAsync(message, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(ContactMessage message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);

        _dbContext.ContactMessages.Update(message);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<ContactMessage>> GetAllAsync(int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        if (pageNumber < 1)
        {
            pageNumber = 1;
        }

        if (pageSize < 1)
        {
            pageSize = 10;
        }

        return await _dbContext.ContactMessages
            .AsNoTracking()
            .Where(m => !m.IsDeleted)
            .OrderByDescending(m => m.CreateDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<ContactMessage>> GetUnreadAsync(int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        if (pageNumber < 1)
        {
            pageNumber = 1;
        }

        if (pageSize < 1)
        {
            pageSize = 10;
        }

        return await _dbContext.ContactMessages
            .AsNoTracking()
            .Where(m => !m.IsDeleted && !m.IsRead)
            .OrderByDescending(m => m.CreateDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<int> GetCountAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.ContactMessages
            .AsNoTracking()
            .Where(m => !m.IsDeleted)
            .CountAsync(cancellationToken);
    }

    public async Task<int> GetUnreadCountAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.ContactMessages
            .AsNoTracking()
            .Where(m => !m.IsDeleted && !m.IsRead)
            .CountAsync(cancellationToken);
    }
}


using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arsis.Application.Interfaces;
using Arsis.Domain.Entities.Blogs;
using Microsoft.EntityFrameworkCore;

namespace Arsis.Infrastructure.Persistence.Repositories;

public sealed class BlogAuthorRepository : IBlogAuthorRepository
{
    private readonly AppDbContext _dbContext;

    public BlogAuthorRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<BlogAuthor>> GetAllAsync(CancellationToken cancellationToken)
        => await _dbContext.BlogAuthors
            .AsNoTracking()
            .Include(author => author.User)
            .Where(author => !author.IsDeleted)
            .ToListAsync(cancellationToken);

    public async Task<BlogAuthor?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => await _dbContext.BlogAuthors
            .AsNoTracking()
            .Include(author => author.User)
            .FirstOrDefaultAsync(author => author.Id == id && !author.IsDeleted, cancellationToken);

    public async Task<BlogAuthor?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken)
        => await _dbContext.BlogAuthors
            .FirstOrDefaultAsync(author => author.Id == id && !author.IsDeleted, cancellationToken);

    public async Task AddAsync(BlogAuthor author, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(author);

        await _dbContext.BlogAuthors.AddAsync(author, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(BlogAuthor author, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(author);

        _dbContext.BlogAuthors.Update(author);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveAsync(BlogAuthor author, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(author);

        _dbContext.BlogAuthors.Update(author);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> ExistsByDisplayNameAsync(string displayName, Guid? excludeId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            return false;
        }

        var normalized = displayName.Trim();

        var query = _dbContext.BlogAuthors
            .AsNoTracking()
            .Where(author => !author.IsDeleted && author.DisplayName == normalized);

        if (excludeId.HasValue)
        {
            var excluded = excludeId.Value;
            query = query.Where(author => author.Id != excluded);
        }

        return await query.AnyAsync(cancellationToken);
    }

    public async Task<bool> ExistsByUserAsync(string userId, Guid? excludeId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return false;
        }

        var normalized = userId.Trim();

        var query = _dbContext.BlogAuthors
            .AsNoTracking()
            .Where(author => !author.IsDeleted && author.UserId == normalized);

        if (excludeId.HasValue)
        {
            var excluded = excludeId.Value;
            query = query.Where(author => author.Id != excluded);
        }

        return await query.AnyAsync(cancellationToken);
    }
}

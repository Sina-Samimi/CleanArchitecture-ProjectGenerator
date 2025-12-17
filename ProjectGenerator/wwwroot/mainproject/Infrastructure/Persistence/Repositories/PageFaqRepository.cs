using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Attar.Application.Interfaces;
using Attar.Domain.Entities.Seo;
using Attar.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Attar.Infrastructure.Persistence.Repositories;

public sealed class PageFaqRepository : IPageFaqRepository
{
    private readonly AppDbContext _dbContext;

    public PageFaqRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PageFaq?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => await _dbContext.PageFaqs
            .AsNoTracking()
            .FirstOrDefaultAsync(faq => faq.Id == id, cancellationToken);

    public async Task<PageFaq?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken)
        => await _dbContext.PageFaqs
            .FirstOrDefaultAsync(faq => faq.Id == id, cancellationToken);

    public async Task<IReadOnlyCollection<PageFaq>> GetByPageTypeAndIdentifierAsync(SeoPageType pageType, string? pageIdentifier, CancellationToken cancellationToken)
    {
        var query = _dbContext.PageFaqs
            .AsNoTracking()
            .Where(faq => faq.PageType == pageType);

        if (string.IsNullOrWhiteSpace(pageIdentifier))
        {
            query = query.Where(faq => faq.PageIdentifier == null);
        }
        else
        {
            query = query.Where(faq => faq.PageIdentifier == pageIdentifier);
        }

        return await query
            .OrderBy(faq => faq.DisplayOrder)
            .ThenBy(faq => faq.CreateDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<PageFaq>> GetAllAsync(CancellationToken cancellationToken)
        => await _dbContext.PageFaqs
            .AsNoTracking()
            .OrderBy(faq => faq.PageType)
            .ThenBy(faq => faq.PageIdentifier ?? string.Empty)
            .ThenBy(faq => faq.DisplayOrder)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(PageFaq pageFaq, CancellationToken cancellationToken)
    {
        await _dbContext.PageFaqs.AddAsync(pageFaq, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(PageFaq pageFaq, CancellationToken cancellationToken)
    {
        var existing = await _dbContext.PageFaqs
            .FirstOrDefaultAsync(faq => faq.Id == pageFaq.Id, cancellationToken);

        if (existing is null)
        {
            return;
        }

        _dbContext.Entry(existing).CurrentValues.SetValues(pageFaq);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(PageFaq pageFaq, CancellationToken cancellationToken)
    {
        _dbContext.PageFaqs.Remove(pageFaq);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}


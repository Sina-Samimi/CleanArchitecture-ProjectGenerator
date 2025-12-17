using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LogTableRenameTest.Application.DTOs;
using LogTableRenameTest.Application.Queries.Admin.PageAccess;
using MediatR;
using Microsoft.Extensions.Caching.Memory;

namespace LogTableRenameTest.WebSite.Services;

public interface IPageAccessCache
{
    Task<IReadOnlyCollection<PageAccessPolicyDto>> GetPoliciesAsync(CancellationToken cancellationToken);

    void Invalidate();
}

public sealed class PageAccessCache : IPageAccessCache
{
    private const string CacheKey = "admin.page-access.policies";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    private readonly IMemoryCache _memoryCache;
    private readonly IMediator _mediator;

    public PageAccessCache(IMemoryCache memoryCache, IMediator mediator)
    {
        _memoryCache = memoryCache;
        _mediator = mediator;
    }

    public async Task<IReadOnlyCollection<PageAccessPolicyDto>> GetPoliciesAsync(CancellationToken cancellationToken)
    {
        if (_memoryCache.TryGetValue(CacheKey, out IReadOnlyCollection<PageAccessPolicyDto>? cached) && cached is not null)
        {
            return cached;
        }

        var result = await _mediator.Send(new GetAllPageAccessPoliciesQuery(), cancellationToken);
        var policies = result.IsSuccess && result.Value is not null
            ? result.Value
            : Array.Empty<PageAccessPolicyDto>();

        _memoryCache.Set(CacheKey, policies, CacheDuration);
        return policies;
    }

    public void Invalidate()
        => _memoryCache.Remove(CacheKey);
}

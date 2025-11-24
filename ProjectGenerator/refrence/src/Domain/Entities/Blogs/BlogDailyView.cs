using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using Arsis.Domain.Base;

namespace Arsis.Domain.Entities.Blogs;

public sealed class BlogDailyView : Entity
{
    public Guid BlogId { get; private set; }

    public Blog Blog { get; private set; } = null!;

    public IPAddress ViewerIp { get; private set; }

    public DateOnly ViewDate { get; private set; }

    [SetsRequiredMembers]
    private BlogDailyView()
    {
        ViewerIp = IPAddress.None;
    }

    [SetsRequiredMembers]
    public BlogDailyView(Guid blogId, IPAddress viewerIp, DateOnly viewDate)
    {
        BlogId = blogId;
        ViewerIp = viewerIp ?? IPAddress.None;
        ViewDate = viewDate;
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void UpdateViewDate(DateOnly viewDate)
    {
        ViewDate = viewDate;
        UpdateDate = DateTimeOffset.UtcNow;
    }
}

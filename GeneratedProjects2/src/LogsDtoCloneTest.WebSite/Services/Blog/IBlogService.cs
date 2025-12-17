using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using LogsDtoCloneTest.WebSite.Models.Blog;

namespace LogsDtoCloneTest.WebSite.Services.Blog;

public interface IBlogService
{
    Task<IReadOnlyList<BlogPost>> GetAllPostsAsync(CancellationToken cancellationToken = default);

    Task<BlogPost?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BlogPost>> GetLatestPostsAsync(int count, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BlogCommentViewModel>> GetCommentsAsync(Guid blogId, CancellationToken cancellationToken = default);

    Task<bool> AddCommentAsync(Guid blogId, string authorName, string content, string? authorEmail, Guid? parentId, CancellationToken cancellationToken = default);

    Task<int> RegisterViewAsync(Guid blogId, IPAddress? viewerIp, CancellationToken cancellationToken = default);
}

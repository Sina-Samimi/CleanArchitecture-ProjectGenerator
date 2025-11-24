using System;
using System.Collections.Generic;

namespace EndPoint.WebSite.Models.Blog;

public class BlogListViewModel
{
    public IReadOnlyList<BlogPostSummaryViewModel> Posts { get; init; } = Array.Empty<BlogPostSummaryViewModel>();
}

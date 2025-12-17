using System;
using System.Collections.Generic;

namespace Attar.WebSite.Models.Blog;

public class BlogListViewModel
{
    public IReadOnlyList<BlogPostSummaryViewModel> Posts { get; init; } = Array.Empty<BlogPostSummaryViewModel>();
}

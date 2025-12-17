using System;
using System.Collections.Generic;
using LogsDtoCloneTest.WebSite.Models.Blog;
using LogsDtoCloneTest.WebSite.Models.Product;

namespace LogsDtoCloneTest.WebSite.Models.Home;

public class HomeViewModel
{
    public IReadOnlyList<BlogPostSummaryViewModel> LatestPosts { get; init; } = Array.Empty<BlogPostSummaryViewModel>();

    public IReadOnlyList<ProductSummaryViewModel> FeaturedProducts { get; init; } = Array.Empty<ProductSummaryViewModel>();
}

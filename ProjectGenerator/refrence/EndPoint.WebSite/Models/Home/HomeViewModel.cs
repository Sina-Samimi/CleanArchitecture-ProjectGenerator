using System;
using System.Collections.Generic;
using EndPoint.WebSite.Models.Blog;
using EndPoint.WebSite.Models.Product;

namespace EndPoint.WebSite.Models.Home;

public class HomeViewModel
{
    public IReadOnlyList<BlogPostSummaryViewModel> LatestPosts { get; init; } = Array.Empty<BlogPostSummaryViewModel>();

    public IReadOnlyList<ProductSummaryViewModel> FeaturedProducts { get; init; } = Array.Empty<ProductSummaryViewModel>();
}

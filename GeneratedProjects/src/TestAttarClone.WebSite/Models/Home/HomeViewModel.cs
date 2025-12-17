using System;
using System.Collections.Generic;
using TestAttarClone.WebSite.Models.Blog;
using TestAttarClone.WebSite.Models.Product;

namespace TestAttarClone.WebSite.Models.Home;

public class HomeViewModel
{
    public IReadOnlyList<BlogPostSummaryViewModel> LatestPosts { get; init; } = Array.Empty<BlogPostSummaryViewModel>();

    public IReadOnlyList<ProductSummaryViewModel> FeaturedProducts { get; init; } = Array.Empty<ProductSummaryViewModel>();
}

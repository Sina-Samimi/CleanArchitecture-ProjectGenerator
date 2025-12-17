using System;
using System.Collections.Generic;
using Attar.WebSite.Models.Blog;
using Attar.WebSite.Models.Product;

namespace Attar.WebSite.Models.Home;

public class HomeViewModel
{
    public IReadOnlyList<BlogPostSummaryViewModel> LatestPosts { get; init; } = Array.Empty<BlogPostSummaryViewModel>();

    public IReadOnlyList<ProductSummaryViewModel> FeaturedProducts { get; init; } = Array.Empty<ProductSummaryViewModel>();
}

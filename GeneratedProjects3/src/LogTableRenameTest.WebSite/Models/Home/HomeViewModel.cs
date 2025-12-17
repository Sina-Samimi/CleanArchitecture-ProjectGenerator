using System;
using System.Collections.Generic;
using LogTableRenameTest.WebSite.Models.Blog;
using LogTableRenameTest.WebSite.Models.Product;

namespace LogTableRenameTest.WebSite.Models.Home;

public class HomeViewModel
{
    public IReadOnlyList<BlogPostSummaryViewModel> LatestPosts { get; init; } = Array.Empty<BlogPostSummaryViewModel>();

    public IReadOnlyList<ProductSummaryViewModel> FeaturedProducts { get; init; } = Array.Empty<ProductSummaryViewModel>();
}

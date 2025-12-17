using System;
using System.Collections.Generic;

namespace LogsDtoCloneTest.WebSite.Models.Blog;

public class BlogDetailViewModel
{
    public required BlogPost Post { get; init; }

    public IReadOnlyList<BlogPostSummaryViewModel> RelatedPosts { get; init; } = Array.Empty<BlogPostSummaryViewModel>();

    public IReadOnlyList<BlogCommentViewModel> Comments { get; init; } = Array.Empty<BlogCommentViewModel>();

    public BlogCommentFormModel NewComment { get; init; } = new();
}

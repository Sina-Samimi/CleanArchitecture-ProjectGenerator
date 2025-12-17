using System;
using System.Collections.Generic;
using System.Globalization;

namespace Attar.WebSite.Models.Blog;

public class BlogCommentViewModel
{
    public Guid Id { get; init; }

    public Guid? ParentId { get; init; }

    public required string AuthorName { get; init; }

    public required string Content { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public IReadOnlyList<BlogCommentViewModel> Replies { get; init; } = Array.Empty<BlogCommentViewModel>();

    public string GetFormattedCreateDate() => CreatedAt.ToLocalTime().ToString("dd MMMM yyyy - HH:mm", new CultureInfo("fa-IR"));

    public bool HasReplies => Replies.Count > 0;
}

using System;
using System.Collections.Generic;

namespace LogsDtoCloneTest.WebSite.Models.Product;

public class ProductCommentViewModel
{
    public Guid Id { get; init; }

    public Guid? ParentId { get; init; }

    public required string AuthorName { get; init; }

    public required string Content { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public double Rating { get; init; }

    public IReadOnlyList<ProductCommentViewModel> Replies { get; init; } = Array.Empty<ProductCommentViewModel>();

    public string GetFormattedDate() => CreatedAt.ToString("dd MMMM yyyy", new System.Globalization.CultureInfo("fa-IR"));
}

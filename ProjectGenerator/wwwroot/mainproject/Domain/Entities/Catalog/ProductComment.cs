using System;
using System.Diagnostics.CodeAnalysis;
using Attar.Domain.Base;
using Attar.Domain.Entities;

namespace Attar.Domain.Entities.Catalog;

public sealed class ProductComment : Entity
{
    private const double MinRating = 0d;
    private const double MaxRating = 5d;

    public Guid ProductId { get; private set; }

    public Product Product { get; private set; } = null!;

    public Guid? ParentId { get; private set; }

    public ProductComment? Parent { get; private set; }

    public string AuthorName { get; private set; }

    public string Content { get; private set; }

    public double Rating { get; private set; }

    public bool IsApproved { get; private set; }

    public string? ApprovedById { get; private set; }

    public ApplicationUser? ApprovedBy { get; private set; }

    public DateTimeOffset? ApprovedAt { get; private set; }

    [SetsRequiredMembers]
    private ProductComment()
    {
        AuthorName = string.Empty;
        Content = string.Empty;
    }

    [SetsRequiredMembers]
    public ProductComment(
        Guid productId,
        string authorName,
        string content,
        double rating,
        ProductComment? parent = null,
        bool isApproved = false)
    {
        if (string.IsNullOrWhiteSpace(authorName))
        {
            throw new ArgumentException("Comment author cannot be empty", nameof(authorName));
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            throw new ArgumentException("Comment content cannot be empty", nameof(content));
        }

        ProductId = productId;
        AuthorName = authorName.Trim();
        Content = content.Trim();
        Rating = NormalizeRating(rating);
        Parent = parent;
        ParentId = parent?.Id;
        IsApproved = isApproved;
        ApprovedById = null;
        ApprovedAt = null;
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void UpdateContent(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new ArgumentException("Comment content cannot be empty", nameof(content));
        }

        Content = content.Trim();
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void UpdateRating(double rating)
    {
        Rating = NormalizeRating(rating);
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void SetApproval(bool isApproved, string? approverId, DateTimeOffset timestamp)
    {
        IsApproved = isApproved;
        if (isApproved && !string.IsNullOrWhiteSpace(approverId))
        {
            ApprovedById = approverId;
            ApprovedAt = timestamp;
        }
        else
        {
            ApprovedById = null;
            ApprovedAt = null;
        }

        UpdateDate = timestamp;
    }

    private static double NormalizeRating(double rating)
    {
        if (double.IsNaN(rating) || double.IsInfinity(rating))
        {
            return MinRating;
        }

        if (rating < MinRating)
        {
            return MinRating;
        }

        if (rating > MaxRating)
        {
            return MaxRating;
        }

        return Math.Round(rating, 1, MidpointRounding.AwayFromZero);
    }
}

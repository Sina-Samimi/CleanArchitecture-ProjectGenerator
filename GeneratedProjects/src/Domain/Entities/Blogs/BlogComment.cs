using System;
using System.Diagnostics.CodeAnalysis;
using TestAttarClone.Domain.Base;
using TestAttarClone.Domain.Entities;

namespace TestAttarClone.Domain.Entities.Blogs;

public sealed class BlogComment : Entity
{
    public Guid BlogId { get; private set; }

    public Blog Blog { get; private set; } = null!;

    public Guid? ParentId { get; private set; }

    public BlogComment? Parent { get; private set; }

    public string AuthorName { get; private set; }

    public string? AuthorEmail { get; private set; }

    public string Content { get; private set; }

    public bool IsApproved { get; private set; }

    public string? ApprovedById { get; private set; }

    public ApplicationUser? ApprovedBy { get; private set; }

    public DateTimeOffset? ApprovedAt { get; private set; }

    [SetsRequiredMembers]
    private BlogComment()
    {
        AuthorName = string.Empty;
        Content = string.Empty;
    }

    [SetsRequiredMembers]
    public BlogComment(Guid blogId, string authorName, string content, string? authorEmail = null, BlogComment? parent = null, bool isApproved = true)
    {
        BlogId = blogId;
        AuthorName = string.IsNullOrWhiteSpace(authorName) ? "کاربر مهمان" : authorName.Trim();
        Content = string.IsNullOrWhiteSpace(content) ? throw new ArgumentException("Comment content cannot be empty", nameof(content)) : content.Trim();
        AuthorEmail = string.IsNullOrWhiteSpace(authorEmail) ? null : authorEmail.Trim();
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
}

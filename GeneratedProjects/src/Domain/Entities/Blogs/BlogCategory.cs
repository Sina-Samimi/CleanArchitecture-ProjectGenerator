using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using TestAttarClone.Domain.Base;

namespace TestAttarClone.Domain.Entities.Blogs;

public sealed class BlogCategory : Entity
{
    private readonly List<BlogCategory> _children = new();

    public string Name { get; private set; }

    public string Slug { get; private set; }

    public string? Description { get; private set; }

    public Guid? ParentId { get; private set; }

    public BlogCategory? Parent { get; private set; }

    public IReadOnlyCollection<BlogCategory> Children => _children.AsReadOnly();

    [SetsRequiredMembers]
    private BlogCategory()
    {
        Name = string.Empty;
        Slug = string.Empty;
    }

    [SetsRequiredMembers]
    public BlogCategory(string name, string slug, string? description = null, BlogCategory? parent = null)
    {
        Update(name, slug, description);
        SetParent(parent);
    }

    public void Update(string name, string slug, string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Category name cannot be empty", nameof(name));
        }

        Name = name.Trim();
        Slug = string.IsNullOrWhiteSpace(slug) ? Name.Replace(' ', '-').ToLowerInvariant() : slug.Trim();
        Description = description?.Trim();
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void SetParent(BlogCategory? parent)
    {
        if (parent is not null && parent.Id == Id)
        {
            throw new InvalidOperationException("A category cannot be parent of itself.");
        }

        Parent = parent;
        ParentId = parent?.Id;
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void AddChild(BlogCategory child)
    {
        ArgumentNullException.ThrowIfNull(child);
        if (!_children.Contains(child))
        {
            _children.Add(child);
        }
    }

    public void RemoveChild(BlogCategory child)
    {
        ArgumentNullException.ThrowIfNull(child);
        _children.Remove(child);
    }
}

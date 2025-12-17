using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using LogTableRenameTest.Domain.Base;
using LogTableRenameTest.Domain.Enums;

namespace LogTableRenameTest.Domain.Entities.Catalog;

public sealed class SiteCategory : Entity
{
    private readonly List<SiteCategory> _children = new();

    public string Name { get; private set; }

    public string Slug { get; private set; }

    public string? Description { get; private set; }

    public string? ImageUrl { get; private set; }

    public CategoryScope Scope { get; private set; }

    public Guid? ParentId { get; private set; }

    public SiteCategory? Parent { get; private set; }

    public IReadOnlyCollection<SiteCategory> Children => _children.AsReadOnly();

    [SetsRequiredMembers]
    private SiteCategory()
    {
        Name = string.Empty;
        Slug = string.Empty;
        Scope = CategoryScope.General;
    }

    [SetsRequiredMembers]
    public SiteCategory(string name, string slug, CategoryScope scope, string? description = null, SiteCategory? parent = null, string? imageUrl = null)
    {
        Update(name, slug, description, scope, imageUrl);
        SetParent(parent);
    }

    public void Update(string name, string slug, string? description, CategoryScope scope, string? imageUrl = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Category name cannot be empty", nameof(name));
        }

        // هیچ محدودیتی برای Scope نیست - همه مقادیر مجاز هستند
        // if (scope is not (CategoryScope.General or CategoryScope.Blog or CategoryScope.Product or CategoryScope.Test))
        // {
        //     throw new ArgumentOutOfRangeException(nameof(scope));
        // }

        Name = name.Trim();
        Slug = string.IsNullOrWhiteSpace(slug)
            ? GenerateSlug(Name)
            : slug.Trim();
        Description = description?.Trim();
        ImageUrl = string.IsNullOrWhiteSpace(imageUrl) ? null : imageUrl.Trim();
        Scope = scope;
        EnsureParentScope(Parent);
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void SetParent(SiteCategory? parent)
    {
        if (parent is not null)
        {
            if (parent.Id == Id)
            {
                throw new InvalidOperationException("A category cannot be parent of itself.");
            }

            if (parent.Scope != Scope)
            {
                throw new InvalidOperationException("Parent category scope must match child scope.");
            }
        }

        Parent = parent;
        ParentId = parent?.Id;
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void AddChild(SiteCategory child)
    {
        ArgumentNullException.ThrowIfNull(child);

        if (child.Scope != Scope)
        {
            throw new InvalidOperationException("Child category scope must match parent scope.");
        }

        if (!_children.Contains(child))
        {
            _children.Add(child);
        }
    }

    public void RemoveChild(SiteCategory child)
    {
        ArgumentNullException.ThrowIfNull(child);
        _children.Remove(child);
    }

    private static string GenerateSlug(string value)
        => value
            .Trim()
            .Replace(" ", "-", StringComparison.Ordinal)
            .ToLowerInvariant();

    private void EnsureParentScope(SiteCategory? parent)
    {
        if (parent is null)
        {
            return;
        }

        if (parent.Scope != Scope)
        {
            throw new InvalidOperationException("Parent category scope must match child scope.");
        }
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using LogsDtoCloneTest.Domain.Base;

namespace LogsDtoCloneTest.Domain.Entities.Navigation;

public sealed class NavigationMenuItem : Entity
{
    private readonly List<NavigationMenuItem> _children = new();

    public Guid? ParentId { get; private set; }

    public NavigationMenuItem? Parent { get; private set; }

    public IReadOnlyCollection<NavigationMenuItem> Children => _children.AsReadOnly();

    public string Title { get; private set; } = string.Empty;

    public string Url { get; private set; } = string.Empty;

    public string Icon { get; private set; } = string.Empty;

    public string? ImageUrl { get; private set; }

    public bool IsVisible { get; private set; }

    public bool OpenInNewTab { get; private set; }

    public int DisplayOrder { get; private set; }

    [SetsRequiredMembers]
    private NavigationMenuItem()
    {
    }

    [SetsRequiredMembers]
    public NavigationMenuItem(
        string title,
        string url,
        string icon,
        bool isVisible,
        bool openInNewTab,
        int displayOrder,
        Guid? parentId,
        string? imageUrl = null)
    {
        UpdateDetails(title, url, icon, isVisible, openInNewTab, displayOrder, imageUrl);
        SetParent(parentId);
        UpdateDate = CreateDate;
    }

    public void UpdateDetails(
        string title,
        string url,
        string icon,
        bool isVisible,
        bool openInNewTab,
        int displayOrder,
        string? imageUrl = null)
    {
        Title = NormalizeRequired(title, nameof(title));
        Url = NormalizeOptional(url);
        Icon = NormalizeOptional(icon);
        ImageUrl = string.IsNullOrWhiteSpace(imageUrl) ? null : imageUrl.Trim();
        IsVisible = isVisible;
        OpenInNewTab = openInNewTab;
        DisplayOrder = EnsureNonNegative(displayOrder, nameof(displayOrder));
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void SetParent(Guid? parentId)
    {
        if (parentId == Id)
        {
            throw new InvalidOperationException("یک آیتم منو نمی‌تواند والد خودش باشد.");
        }

        ParentId = parentId;
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void AttachChild(NavigationMenuItem child)
    {
        ArgumentNullException.ThrowIfNull(child);
        _children.Add(child);
    }

    private static string NormalizeRequired(string value, string argumentName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, argumentName);
        return value.Trim();
    }

    private static string NormalizeOptional(string value)
        => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();

    private static int EnsureNonNegative(int value, string argumentName)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(argumentName, value, "مقدار نمی‌تواند منفی باشد.");
        }

        return value;
    }
}

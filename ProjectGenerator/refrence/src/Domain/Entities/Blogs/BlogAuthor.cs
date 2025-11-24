using System;
using System.Diagnostics.CodeAnalysis;
using Arsis.Domain.Base;
using Arsis.Domain.Entities;

namespace Arsis.Domain.Entities.Blogs;

public sealed class BlogAuthor : Entity
{
    public string DisplayName { get; private set; }

    public string? Bio { get; private set; }

    public string? AvatarUrl { get; private set; }

    public bool IsActive { get; private set; }

    public string? UserId { get; private set; }

    public ApplicationUser? User { get; private set; }

    [SetsRequiredMembers]
    private BlogAuthor()
    {
        DisplayName = string.Empty;
        IsActive = true;
    }

    [SetsRequiredMembers]
    public BlogAuthor(string displayName, string? bio = null, string? avatarUrl = null, bool isActive = true, string? userId = null)
    {
        Update(displayName, bio, avatarUrl, isActive, userId);
    }

    public void Update(string displayName, string? bio, string? avatarUrl, bool isActive, string? userId)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("Author display name cannot be empty", nameof(displayName));
        }

        DisplayName = displayName.Trim();
        Bio = bio?.Trim();
        AvatarUrl = string.IsNullOrWhiteSpace(avatarUrl) ? null : avatarUrl.Trim();
        IsActive = isActive;
        UserId = string.IsNullOrWhiteSpace(userId) ? null : userId.Trim();
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void Activate()
    {
        if (!IsActive)
        {
            IsActive = true;
            UpdateDate = DateTimeOffset.UtcNow;
        }
    }

    public void Deactivate()
    {
        if (IsActive)
        {
            IsActive = false;
            UpdateDate = DateTimeOffset.UtcNow;
        }
    }
}

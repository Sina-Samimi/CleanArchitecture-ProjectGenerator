using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using LogsDtoCloneTest.Domain.Base;

namespace LogsDtoCloneTest.Domain.Entities;

public sealed class AccessPermission : Entity
{
    private DateTimeOffset? _updatedAt;

    public string Key { get; private set; } = string.Empty;

    public string DisplayName { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public bool IsCore { get; private set; }

    public string GroupKey { get; private set; } = "custom";

    public string GroupDisplayName { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAt
    {
        get => CreateDate;
        private set => CreateDate = value;
    }

    public DateTimeOffset? UpdatedAt
    {
        get => _updatedAt;
        private set
        {
            _updatedAt = value;
            if (value.HasValue)
            {
                UpdateDate = value.Value;
            }
        }
    }

    [SetsRequiredMembers]
    private AccessPermission()
    {
    }

    [SetsRequiredMembers]
    public AccessPermission(
        string key,
        string displayName,
        string? description,
        bool isCore,
        string groupKey,
        string groupDisplayName)
    {
        Key = key;
        DisplayName = displayName;
        Description = description;
        IsCore = isCore;
        GroupKey = string.IsNullOrWhiteSpace(groupKey) ? "custom" : groupKey;
        GroupDisplayName = string.IsNullOrWhiteSpace(groupDisplayName)
            ? GroupKey
            : groupDisplayName;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = null;
        Ip = IPAddress.None;
    }

    public void UpdateDetails(
        string displayName,
        string? description,
        bool isCore,
        string groupKey,
        string groupDisplayName)
    {
        DisplayName = displayName;
        Description = description;
        IsCore = isCore;
        if (!string.IsNullOrWhiteSpace(groupKey))
        {
            GroupKey = groupKey;
        }
        if (!string.IsNullOrWhiteSpace(groupDisplayName))
        {
            GroupDisplayName = groupDisplayName;
        }
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}

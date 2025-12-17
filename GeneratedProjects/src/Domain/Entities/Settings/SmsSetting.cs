using System;
using System.Diagnostics.CodeAnalysis;
using TestAttarClone.Domain.Base;

namespace TestAttarClone.Domain.Entities.Settings;

public sealed class SmsSetting : Entity
{
    public string ApiKey { get; private set; } = string.Empty;

    public bool IsActive { get; private set; }

    [SetsRequiredMembers]
    private SmsSetting()
    {
    }

    [SetsRequiredMembers]
    public SmsSetting(
        string apiKey,
        bool isActive = true)
    {
        Update(apiKey, isActive);
    }

    public void Update(
        string apiKey,
        bool isActive = true)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new ArgumentException("API Key cannot be empty", nameof(apiKey));
        }

        ApiKey = apiKey.Trim();
        IsActive = isActive;
        
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

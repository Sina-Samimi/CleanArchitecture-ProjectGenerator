using System;
using System.Diagnostics.CodeAnalysis;
using Arsis.Domain.Base;
using Arsis.Domain.Interfaces;

namespace Arsis.Domain.Entities;

public sealed class Organization : BaseEntity<Guid>, IAggregateRoot
{
    [SetsRequiredMembers]
    private Organization()
    {
    }

    [SetsRequiredMembers]
    public Organization(string name, string code, string adminName, string adminEmail)
    {
        Name = name;
        Code = code.ToUpperInvariant();
        AdminName = adminName;
        AdminEmail = adminEmail;
        Status = OrganizationStatus.Active;
    }

    public string Name { get; private set; } = string.Empty;

    public string Code { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public string AdminName { get; private set; } = string.Empty;

    public string AdminEmail { get; private set; } = string.Empty;

    public string? PhoneNumber { get; private set; }

    public string? Address { get; private set; }

    public OrganizationStatus Status { get; private set; }

    public int MaxUsers { get; private set; } = 100;

    public DateTimeOffset? SubscriptionExpiry { get; private set; }

    public void UpdateDetails(string name, string description, string adminName, string adminEmail, string? phoneNumber, string? address)
    {
        Name = name;
        Description = description;
        AdminName = adminName;
        AdminEmail = adminEmail;
        PhoneNumber = phoneNumber;
        Address = address;
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void UpdateStatus(OrganizationStatus status)
    {
        Status = status;
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void UpdateSubscription(int maxUsers, DateTimeOffset? expiry)
    {
        MaxUsers = maxUsers;
        SubscriptionExpiry = expiry;
        UpdateDate = DateTimeOffset.UtcNow;
    }
}

public enum OrganizationStatus
{
    Active = 1,
    Inactive = 2,
    Suspended = 3,
    Expired = 4
}

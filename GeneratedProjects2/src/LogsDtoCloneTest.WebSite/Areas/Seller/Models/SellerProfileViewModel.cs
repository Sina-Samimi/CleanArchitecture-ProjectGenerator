using System;

namespace LogsDtoCloneTest.WebSite.Areas.Seller.Models;

public sealed class SellerProfileViewModel
{
    public Guid Id { get; init; }

    public string DisplayName { get; init; } = string.Empty;

    public string? LicenseNumber { get; init; }

    public DateOnly? LicenseIssueDate { get; init; }

    public DateOnly? LicenseExpiryDate { get; init; }

    public string? ShopAddress { get; init; }

    public string? WorkingHours { get; init; }

    public int? ExperienceYears { get; init; }

    public string? Bio { get; init; }

    public string? AvatarUrl { get; init; }

    public string? ContactEmail { get; init; }

    public string? ContactPhone { get; init; }

    public bool IsActive { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset UpdatedAt { get; init; }
}


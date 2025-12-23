using System;
using System.Diagnostics.CodeAnalysis;
using MobiRooz.Domain.Base;
using MobiRooz.Domain.Entities;

namespace MobiRooz.Domain.Entities.Sellers;

public sealed class SellerProfile : Entity
{
    public string DisplayName { get; private set; }

    public string? LicenseNumber { get; private set; }

    public DateOnly? LicenseIssueDate { get; private set; }

    public DateOnly? LicenseExpiryDate { get; private set; }

    public string? ShopAddress { get; private set; }

    public string? WorkingHours { get; private set; }

    public int? ExperienceYears { get; private set; }

    public string? Bio { get; private set; }

    public string? AvatarUrl { get; private set; }

    public string? ContactEmail { get; private set; }

    public string? ContactPhone { get; private set; }

    public string? UserId { get; private set; }

    public bool IsActive { get; private set; }

    public decimal? SellerSharePercentage { get; private set; }

    public ApplicationUser? User { get; private set; }

    [SetsRequiredMembers]
    private SellerProfile()
    {
        DisplayName = string.Empty;
    }

    [SetsRequiredMembers]
    public SellerProfile(
        string displayName,
        string? licenseNumber,
        DateOnly? licenseIssueDate,
        DateOnly? licenseExpiryDate,
        string? shopAddress,
        string? workingHours,
        int? experienceYears,
        string? bio,
        string? avatarUrl,
        string? contactEmail,
        string? contactPhone,
        string? userId,
        bool isActive = true,
        decimal? sellerSharePercentage = null)
    {
        UpdateDisplayName(displayName);
        UpdateBusinessInfo(licenseNumber, licenseIssueDate, licenseExpiryDate, shopAddress, workingHours, experienceYears, bio);
        UpdateMedia(avatarUrl);
        UpdateContact(contactEmail, contactPhone);
        ConnectToUser(userId);
        SetSellerSharePercentage(sellerSharePercentage);
        IsActive = isActive;
    }

    public void UpdateDisplayName(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("نام فروشنده الزامی است.", nameof(displayName));
        }

        DisplayName = displayName.Trim();
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void UpdateBusinessInfo(
        string? licenseNumber,
        DateOnly? licenseIssueDate,
        DateOnly? licenseExpiryDate,
        string? shopAddress,
        string? workingHours,
        int? experienceYears,
        string? bio)
    {
        LicenseNumber = NormalizeOptional(licenseNumber);
        LicenseIssueDate = licenseIssueDate;
        LicenseExpiryDate = licenseExpiryDate;
        ShopAddress = NormalizeOptional(shopAddress);
        WorkingHours = NormalizeOptional(workingHours);
        ExperienceYears = experienceYears;
        Bio = NormalizeOptional(bio);
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void UpdateMedia(string? avatarUrl)
    {
        AvatarUrl = NormalizeOptional(avatarUrl);
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void UpdateContact(string? email, string? phone)
    {
        ContactEmail = NormalizeOptional(email);
        ContactPhone = NormalizeOptional(phone);
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void ConnectToUser(string? userId)
    {
        UserId = NormalizeOptional(userId);
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void SetActive(bool isActive)
    {
        IsActive = isActive;
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void SetSellerSharePercentage(decimal? sellerSharePercentage)
    {
        if (sellerSharePercentage.HasValue)
        {
            if (sellerSharePercentage.Value < 0 || sellerSharePercentage.Value > 100)
            {
                throw new ArgumentException("درصد فروش باید بین ۰ تا ۱۰۰ باشد.", nameof(sellerSharePercentage));
            }
        }

        SellerSharePercentage = sellerSharePercentage;
        UpdateDate = DateTimeOffset.UtcNow;
    }

    private static string? NormalizeOptional(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }
}

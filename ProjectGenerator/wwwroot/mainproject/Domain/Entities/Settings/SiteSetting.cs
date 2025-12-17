using System;
using System.Diagnostics.CodeAnalysis;
using Attar.Domain.Base;

namespace Attar.Domain.Entities.Settings;

public sealed class SiteSetting : Entity
{
    public string SiteTitle { get; private set; } = string.Empty;

    public string SiteEmail { get; private set; } = string.Empty;

    public string SupportEmail { get; private set; } = string.Empty;

    public string ContactPhone { get; private set; } = string.Empty;

    public string SupportPhone { get; private set; } = string.Empty;

    public string Address { get; private set; } = string.Empty;

    public string ContactDescription { get; private set; } = string.Empty;

    public string? LogoPath { get; private set; }

    public string? FaviconPath { get; private set; }

    public string? ShortDescription { get; private set; }

    public string? TermsAndConditions { get; private set; }

    public string? HeroBannerPath { get; private set; }

    public string? TelegramUrl { get; private set; }

    public string? InstagramUrl { get; private set; }

    public string? WhatsAppUrl { get; private set; }

    public string? LinkedInUrl { get; private set; }

    public bool BannersAsSlider { get; private set; }

    [SetsRequiredMembers]
    private SiteSetting()
    {
    }

    [SetsRequiredMembers]
    public SiteSetting(
        string siteTitle,
        string siteEmail,
        string supportEmail,
        string contactPhone,
        string supportPhone,
        string address,
        string contactDescription,
        string? logoPath = null,
        string? faviconPath = null,
        string? shortDescription = null,
        string? termsAndConditions = null,
        string? heroBannerPath = null,
        string? telegramUrl = null,
        string? instagramUrl = null,
        string? whatsAppUrl = null,
        string? linkedInUrl = null,
        bool bannersAsSlider = false)
    {
        ApplyValues(
            siteTitle,
            siteEmail,
            supportEmail,
            contactPhone,
            supportPhone,
            address,
            contactDescription,
            logoPath,
            faviconPath,
            shortDescription,
            termsAndConditions,
            heroBannerPath,
            telegramUrl,
            instagramUrl,
            whatsAppUrl,
            linkedInUrl,
            bannersAsSlider,
            true);
    }

    public void Update(
        string siteTitle,
        string siteEmail,
        string supportEmail,
        string contactPhone,
        string supportPhone,
        string address,
        string contactDescription,
        string? logoPath = null,
        string? faviconPath = null,
        string? shortDescription = null,
        string? termsAndConditions = null,
        string? heroBannerPath = null,
        string? telegramUrl = null,
        string? instagramUrl = null,
        string? whatsAppUrl = null,
        string? linkedInUrl = null,
        bool bannersAsSlider = false)
        => ApplyValues(
            siteTitle,
            siteEmail,
            supportEmail,
            contactPhone,
            supportPhone,
            address,
            contactDescription,
            logoPath,
            faviconPath,
            shortDescription,
            termsAndConditions,
            heroBannerPath,
            telegramUrl,
            instagramUrl,
            whatsAppUrl,
            linkedInUrl,
            bannersAsSlider,
            false);

    private void ApplyValues(
        string siteTitle,
        string siteEmail,
        string supportEmail,
        string contactPhone,
        string supportPhone,
        string address,
        string contactDescription,
        string? logoPath,
        string? faviconPath,
        string? shortDescription,
        string? termsAndConditions,
        string? heroBannerPath,
        string? telegramUrl,
        string? instagramUrl,
        string? whatsAppUrl,
        string? linkedInUrl,
        bool bannersAsSlider,
        bool initializing)
    {
        SiteTitle = NormalizeRequired(siteTitle, nameof(siteTitle));
        SiteEmail = NormalizeOptional(siteEmail);
        SupportEmail = NormalizeOptional(supportEmail);
        ContactPhone = NormalizeOptional(contactPhone);
        SupportPhone = NormalizeOptional(supportPhone);
        Address = NormalizeOptional(address);
        ContactDescription = NormalizeOptional(contactDescription);
        LogoPath = NormalizeOptionalNullable(logoPath);
        FaviconPath = NormalizeOptionalNullable(faviconPath);
        ShortDescription = NormalizeOptionalNullable(shortDescription);
        TermsAndConditions = NormalizeOptionalNullable(termsAndConditions);
        HeroBannerPath = NormalizeOptionalNullable(heroBannerPath);
        TelegramUrl = NormalizeOptionalNullable(telegramUrl);
        InstagramUrl = NormalizeOptionalNullable(instagramUrl);
        WhatsAppUrl = NormalizeOptionalNullable(whatsAppUrl);
        LinkedInUrl = NormalizeOptionalNullable(linkedInUrl);
        BannersAsSlider = bannersAsSlider;

        if (!initializing)
        {
            UpdateDate = DateTimeOffset.UtcNow;
        }
    }

    private static string NormalizeRequired(string value, string argumentName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, argumentName);
        return value.Trim();
    }

    private static string NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();

    private static string? NormalizeOptionalNullable(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

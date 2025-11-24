using System;
using System.Diagnostics.CodeAnalysis;
using Arsis.Domain.Base;

namespace Arsis.Domain.Entities.Settings;

public sealed class SiteSetting : Entity
{
    public string SiteTitle { get; private set; } = string.Empty;

    public string SiteEmail { get; private set; } = string.Empty;

    public string SupportEmail { get; private set; } = string.Empty;

    public string ContactPhone { get; private set; } = string.Empty;

    public string SupportPhone { get; private set; } = string.Empty;

    public string Address { get; private set; } = string.Empty;

    public string ContactDescription { get; private set; } = string.Empty;

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
        string contactDescription)
    {
        ApplyValues(
            siteTitle,
            siteEmail,
            supportEmail,
            contactPhone,
            supportPhone,
            address,
            contactDescription,
            true);
    }

    public void Update(
        string siteTitle,
        string siteEmail,
        string supportEmail,
        string contactPhone,
        string supportPhone,
        string address,
        string contactDescription)
        => ApplyValues(
            siteTitle,
            siteEmail,
            supportEmail,
            contactPhone,
            supportPhone,
            address,
            contactDescription,
            false);

    private void ApplyValues(
        string siteTitle,
        string siteEmail,
        string supportEmail,
        string contactPhone,
        string supportPhone,
        string address,
        string contactDescription,
        bool initializing)
    {
        SiteTitle = NormalizeRequired(siteTitle, nameof(siteTitle));
        SiteEmail = NormalizeOptional(siteEmail);
        SupportEmail = NormalizeOptional(supportEmail);
        ContactPhone = NormalizeOptional(contactPhone);
        SupportPhone = NormalizeOptional(supportPhone);
        Address = NormalizeOptional(address);
        ContactDescription = NormalizeOptional(contactDescription);

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

    private static string NormalizeOptional(string value)
        => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
}

namespace Arsis.Application.DTOs.Settings;

public sealed record SiteSettingDto(
    string SiteTitle,
    string SiteEmail,
    string SupportEmail,
    string ContactPhone,
    string SupportPhone,
    string Address,
    string ContactDescription);

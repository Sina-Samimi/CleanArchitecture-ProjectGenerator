namespace TestAttarClone.Application.DTOs.Settings;

public sealed record SiteSettingDto(
    string SiteTitle,
    string SiteEmail,
    string SupportEmail,
    string ContactPhone,
    string SupportPhone,
    string Address,
    string ContactDescription,
    string? LogoPath,
    string? FaviconPath,
    string? ShortDescription,
    string? TermsAndConditions,
    string? TelegramUrl,
    string? InstagramUrl,
    string? WhatsAppUrl,
    string? LinkedInUrl,
    bool BannersAsSlider);

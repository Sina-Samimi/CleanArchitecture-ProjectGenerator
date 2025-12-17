namespace Attar.Application.DTOs.Settings;

public sealed record SmsSettingDto(
    string ApiKey,
    bool IsActive);

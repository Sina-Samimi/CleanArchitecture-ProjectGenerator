namespace TestAttarClone.Application.DTOs.Settings;

public sealed record AboutSettingDto(
    string Title,
    string Description,
    string? Vision,
    string? Mission,
    string? ImagePath,
    string? MetaTitle,
    string? MetaDescription);


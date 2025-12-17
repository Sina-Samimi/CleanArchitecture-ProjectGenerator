using System;
using System.Diagnostics.CodeAnalysis;
using LogTableRenameTest.Domain.Base;

namespace LogTableRenameTest.Domain.Entities.Settings;

public sealed class AboutSetting : Entity
{
    public string Title { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public string? Vision { get; private set; }

    public string? Mission { get; private set; }

    public string? ImagePath { get; private set; }

    public string? MetaTitle { get; private set; }

    public string? MetaDescription { get; private set; }

    [SetsRequiredMembers]
    private AboutSetting()
    {
    }

    [SetsRequiredMembers]
    public AboutSetting(
        string title,
        string description,
        string? vision = null,
        string? mission = null,
        string? imagePath = null,
        string? metaTitle = null,
        string? metaDescription = null)
    {
        ApplyValues(title, description, vision, mission, imagePath, metaTitle, metaDescription, true);
    }

    public void Update(
        string title,
        string description,
        string? vision = null,
        string? mission = null,
        string? imagePath = null,
        string? metaTitle = null,
        string? metaDescription = null)
        => ApplyValues(title, description, vision, mission, imagePath, metaTitle, metaDescription, false);

    private void ApplyValues(
        string title,
        string description,
        string? vision,
        string? mission,
        string? imagePath,
        string? metaTitle,
        string? metaDescription,
        bool initializing)
    {
        Title = NormalizeRequired(title, nameof(title));
        Description = NormalizeOptional(description);
        Vision = NormalizeOptionalNullable(vision);
        Mission = NormalizeOptionalNullable(mission);
        ImagePath = NormalizeOptionalNullable(imagePath);
        MetaTitle = NormalizeOptionalNullable(metaTitle);
        MetaDescription = NormalizeOptionalNullable(metaDescription);

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


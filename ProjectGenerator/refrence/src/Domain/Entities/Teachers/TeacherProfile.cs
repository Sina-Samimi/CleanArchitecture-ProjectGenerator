using System;
using System.Diagnostics.CodeAnalysis;
using Arsis.Domain.Base;
using Arsis.Domain.Entities;

namespace Arsis.Domain.Entities.Teachers;

public sealed class TeacherProfile : Entity
{
    public string DisplayName { get; private set; }

    public string? Degree { get; private set; }

    public string? Specialty { get; private set; }

    public string? Bio { get; private set; }

    public string? AvatarUrl { get; private set; }

    public string? ContactEmail { get; private set; }

    public string? ContactPhone { get; private set; }

    public string? UserId { get; private set; }

    public bool IsActive { get; private set; }

    public ApplicationUser? User { get; private set; }

    [SetsRequiredMembers]
    private TeacherProfile()
    {
        DisplayName = string.Empty;
    }

    [SetsRequiredMembers]
    public TeacherProfile(
        string displayName,
        string? degree,
        string? specialty,
        string? bio,
        string? avatarUrl,
        string? contactEmail,
        string? contactPhone,
        string? userId,
        bool isActive = true)
    {
        UpdateDisplayName(displayName);
        UpdateAcademicInfo(degree, specialty, bio);
        UpdateMedia(avatarUrl);
        UpdateContact(contactEmail, contactPhone);
        ConnectToUser(userId);
        IsActive = isActive;
    }

    public void UpdateDisplayName(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("نام مدرس الزامی است.", nameof(displayName));
        }

        DisplayName = displayName.Trim();
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void UpdateAcademicInfo(string? degree, string? specialty, string? bio)
    {
        Degree = NormalizeOptional(degree);
        Specialty = NormalizeOptional(specialty);
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

    private static string? NormalizeOptional(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }
}

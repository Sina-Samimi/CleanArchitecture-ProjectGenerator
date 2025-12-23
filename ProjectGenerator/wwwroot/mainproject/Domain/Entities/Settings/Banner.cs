using System;
using System.Diagnostics.CodeAnalysis;
using MobiRooz.Domain.Base;

namespace MobiRooz.Domain.Entities.Settings;

public sealed class Banner : Entity
{
    public string Title { get; private set; } = string.Empty;

    public string ImagePath { get; private set; } = string.Empty;

    public string? LinkUrl { get; private set; }

    public string? AltText { get; private set; }

    public int DisplayOrder { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset? StartDate { get; private set; }

    public DateTimeOffset? EndDate { get; private set; }

    public bool ShowOnHomePage { get; private set; }

    [SetsRequiredMembers]
    private Banner()
    {
    }

    [SetsRequiredMembers]
    public Banner(
        string title,
        string imagePath,
        string? linkUrl = null,
        string? altText = null,
        int displayOrder = 0,
        bool isActive = true,
        DateTimeOffset? startDate = null,
        DateTimeOffset? endDate = null,
        bool showOnHomePage = true)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Title cannot be empty", nameof(title));
        }

        if (string.IsNullOrWhiteSpace(imagePath))
        {
            throw new ArgumentException("Image path cannot be empty", nameof(imagePath));
        }

        if (startDate.HasValue && endDate.HasValue && startDate.Value > endDate.Value)
        {
            throw new ArgumentException("Start date cannot be after end date");
        }

        Title = title.Trim();
        ImagePath = imagePath.Trim();
        LinkUrl = string.IsNullOrWhiteSpace(linkUrl) ? null : linkUrl.Trim();
        AltText = string.IsNullOrWhiteSpace(altText) ? null : altText.Trim();
        DisplayOrder = displayOrder;
        IsActive = isActive;
        StartDate = startDate;
        EndDate = endDate;
        ShowOnHomePage = showOnHomePage;
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void UpdateTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Title cannot be empty", nameof(title));
        }

        Title = title.Trim();
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void UpdateImagePath(string imagePath)
    {
        if (string.IsNullOrWhiteSpace(imagePath))
        {
            throw new ArgumentException("Image path cannot be empty", nameof(imagePath));
        }

        ImagePath = imagePath.Trim();
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void UpdateLinkUrl(string? linkUrl)
    {
        LinkUrl = string.IsNullOrWhiteSpace(linkUrl) ? null : linkUrl.Trim();
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void UpdateAltText(string? altText)
    {
        AltText = string.IsNullOrWhiteSpace(altText) ? null : altText.Trim();
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void UpdateDisplayOrder(int displayOrder)
    {
        DisplayOrder = displayOrder;
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void UpdateDates(DateTimeOffset? startDate, DateTimeOffset? endDate)
    {
        if (startDate.HasValue && endDate.HasValue && startDate.Value > endDate.Value)
        {
            throw new ArgumentException("Start date cannot be after end date");
        }

        StartDate = startDate;
        EndDate = endDate;
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void SetShowOnHomePage(bool showOnHomePage)
    {
        ShowOnHomePage = showOnHomePage;
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void Update(
        string title,
        string imagePath,
        string? linkUrl,
        string? altText,
        int displayOrder,
        bool isActive,
        DateTimeOffset? startDate,
        DateTimeOffset? endDate,
        bool showOnHomePage)
    {
        UpdateTitle(title);
        UpdateImagePath(imagePath);
        UpdateLinkUrl(linkUrl);
        UpdateAltText(altText);
        UpdateDisplayOrder(displayOrder);
        UpdateDates(startDate, endDate);
        SetShowOnHomePage(showOnHomePage);

        if (isActive)
        {
            Activate();
        }
        else
        {
            Deactivate();
        }
    }

    public bool IsCurrentlyActive()
    {
        if (!IsActive)
        {
            return false;
        }

        var now = DateTimeOffset.UtcNow;

        if (StartDate.HasValue && now < StartDate.Value)
        {
            return false;
        }

        if (EndDate.HasValue && now > EndDate.Value)
        {
            return false;
        }

        return true;
    }

    public void Delete()
    {
        IsDeleted = true;
        UpdateDate = DateTimeOffset.UtcNow;
    }
}


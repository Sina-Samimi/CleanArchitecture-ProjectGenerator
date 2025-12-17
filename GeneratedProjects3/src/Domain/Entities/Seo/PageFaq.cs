using System;
using System.Diagnostics.CodeAnalysis;
using LogTableRenameTest.Domain.Base;
using LogTableRenameTest.Domain.Enums;

namespace LogTableRenameTest.Domain.Entities.Seo;

public sealed class PageFaq : Entity
{
    private const int MaxQuestionLength = 500;
    private const int MaxAnswerLength = 3000;

    public SeoPageType PageType { get; private set; }

    public string? PageIdentifier { get; private set; }

    public string Question { get; private set; } = string.Empty;

    public string Answer { get; private set; } = string.Empty;

    public int DisplayOrder { get; private set; }

    [SetsRequiredMembers]
    private PageFaq()
    {
    }

    [SetsRequiredMembers]
    public PageFaq(
        SeoPageType pageType,
        string question,
        string answer,
        int displayOrder,
        string? pageIdentifier = null)
    {
        UpdateDetails(pageType, question, answer, displayOrder, pageIdentifier);
    }

    public void UpdateDetails(
        SeoPageType pageType,
        string question,
        string answer,
        int displayOrder,
        string? pageIdentifier = null)
    {
        if (string.IsNullOrWhiteSpace(question))
        {
            throw new ArgumentException("Question cannot be empty", nameof(question));
        }

        if (string.IsNullOrWhiteSpace(answer))
        {
            throw new ArgumentException("Answer cannot be empty", nameof(answer));
        }

        PageType = pageType;
        PageIdentifier = string.IsNullOrWhiteSpace(pageIdentifier) ? null : pageIdentifier.Trim();
        Question = Normalize(question, MaxQuestionLength);
        Answer = Normalize(answer, MaxAnswerLength);
        DisplayOrder = Math.Max(0, displayOrder);
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void UpdateOrder(int displayOrder)
    {
        DisplayOrder = Math.Max(0, displayOrder);
        UpdateDate = DateTimeOffset.UtcNow;
    }

    private static string Normalize(string value, int maxLength)
    {
        var trimmed = value.Trim();
        return trimmed.Length > maxLength ? trimmed[..maxLength] : trimmed;
    }
}


using System;
using System.Diagnostics.CodeAnalysis;
using LogTableRenameTest.Domain.Base;

namespace LogTableRenameTest.Domain.Entities.Catalog;

public sealed class ProductFaq : Entity
{
    private const int MaxQuestionLength = 300;
    private const int MaxAnswerLength = 2000;

    public Guid ProductId { get; private set; }

    public Product Product { get; private set; } = null!;

    public string Question { get; private set; } = string.Empty;

    public string Answer { get; private set; } = string.Empty;

    public int DisplayOrder { get; private set; }

    [SetsRequiredMembers]
    private ProductFaq()
    {
    }

    [SetsRequiredMembers]
    public ProductFaq(Guid productId, string question, string answer, int displayOrder)
    {
        ProductId = productId;
        UpdateDetails(question, answer, displayOrder);
    }

    public void UpdateDetails(string question, string answer, int displayOrder)
    {
        if (string.IsNullOrWhiteSpace(question))
        {
            throw new ArgumentException("Question cannot be empty", nameof(question));
        }

        if (string.IsNullOrWhiteSpace(answer))
        {
            throw new ArgumentException("Answer cannot be empty", nameof(answer));
        }

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

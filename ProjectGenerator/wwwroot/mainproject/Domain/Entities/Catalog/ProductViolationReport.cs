using System;
using System.Diagnostics.CodeAnalysis;
using Attar.Domain.Base;

namespace Attar.Domain.Entities.Catalog;

public sealed class ProductViolationReport : Entity
{
    public Guid ProductId { get; private set; }

    public Product Product { get; private set; } = null!;

    public Guid? ProductOfferId { get; private set; }

    public ProductOffer? ProductOffer { get; private set; }

    public string? SellerId { get; private set; }

    public string Subject { get; private set; }

    public string Message { get; private set; }

    public string ReporterId { get; private set; }

    public string ReporterPhone { get; private set; }

    public bool IsReviewed { get; private set; }

    public string? ReviewedById { get; private set; }

    public DateTimeOffset? ReviewedAt { get; private set; }

    [SetsRequiredMembers]
    private ProductViolationReport()
    {
        Subject = string.Empty;
        Message = string.Empty;
        ReporterId = string.Empty;
        ReporterPhone = string.Empty;
    }

    [SetsRequiredMembers]
    public ProductViolationReport(
        Guid productId,
        string subject,
        string message,
        string reporterId,
        string reporterPhone,
        Guid? productOfferId = null,
        string? sellerId = null)
    {
        if (productId == Guid.Empty)
        {
            throw new ArgumentException("Product ID cannot be empty", nameof(productId));
        }

        if (string.IsNullOrWhiteSpace(subject))
        {
            throw new ArgumentException("Subject cannot be empty", nameof(subject));
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("Message cannot be empty", nameof(message));
        }

        if (string.IsNullOrWhiteSpace(reporterId))
        {
            throw new ArgumentException("Reporter ID cannot be empty", nameof(reporterId));
        }

        if (string.IsNullOrWhiteSpace(reporterPhone))
        {
            throw new ArgumentException("Reporter phone cannot be empty", nameof(reporterPhone));
        }

        ProductId = productId;
        ProductOfferId = productOfferId;
        SellerId = sellerId?.Trim();
        Subject = subject.Trim();
        Message = message.Trim();
        ReporterId = reporterId.Trim();
        ReporterPhone = reporterPhone.Trim();
        IsReviewed = false;
        ReviewedById = null;
        ReviewedAt = null;
        CreateDate = DateTimeOffset.UtcNow;
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void MarkAsReviewed(string reviewedById)
    {
        if (string.IsNullOrWhiteSpace(reviewedById))
        {
            throw new ArgumentException("Reviewed by ID cannot be empty", nameof(reviewedById));
        }

        IsReviewed = true;
        ReviewedById = reviewedById.Trim();
        ReviewedAt = DateTimeOffset.UtcNow;
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void UnmarkAsReviewed()
    {
        IsReviewed = false;
        ReviewedById = null;
        ReviewedAt = null;
        UpdateDate = DateTimeOffset.UtcNow;
    }
}


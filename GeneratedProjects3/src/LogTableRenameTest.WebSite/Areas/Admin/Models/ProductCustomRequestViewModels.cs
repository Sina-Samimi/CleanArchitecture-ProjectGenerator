using System;
using System.Collections.Generic;
using LogTableRenameTest.Domain.Enums;

namespace LogTableRenameTest.WebSite.Areas.Admin.Models;

public sealed class ProductCustomRequestListViewModel
{
    public IReadOnlyCollection<ProductCustomRequestViewModel> Requests { get; init; }
        = Array.Empty<ProductCustomRequestViewModel>();

    public int TotalCount { get; init; }

    public int PageNumber { get; init; }

    public int PageSize { get; init; }

    public CustomRequestStatus? SelectedStatus { get; init; }

    public Guid? SelectedProductId { get; init; }
}

public sealed class ProductCustomRequestViewModel
{
    public Guid Id { get; init; }

    public Guid ProductId { get; init; }

    public string ProductName { get; init; } = string.Empty;

    public string? UserId { get; init; }

    public string FullName { get; init; } = string.Empty;

    public string Phone { get; init; } = string.Empty;

    public string? Email { get; init; }

    public string? Message { get; init; }

    public CustomRequestStatus Status { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset? ContactedAt { get; init; }

    public string? AdminNotes { get; init; }
}


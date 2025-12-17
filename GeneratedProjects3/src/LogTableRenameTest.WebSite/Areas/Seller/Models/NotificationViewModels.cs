using System;
using System.Collections.Generic;
using LogTableRenameTest.Domain.Enums;

namespace LogTableRenameTest.WebSite.Areas.Seller.Models;

public sealed class NotificationListViewModel
{
    public IReadOnlyCollection<NotificationItemViewModel> Items { get; set; } = Array.Empty<NotificationItemViewModel>();
    public int TotalCount { get; set; }
    public int UnreadCount { get; set; }
    public bool? IsReadFilter { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalPages { get; set; }
    public NotificationFilterViewModel Filter { get; set; } = new();
}

public sealed class NotificationItemViewModel
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public NotificationPriority Priority { get; set; }
    public DateTimeOffset SentAt { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public string CreatedByDisplayName { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTimeOffset? ReadAt { get; set; }
}

public sealed class NotificationFilterViewModel
{
    public NotificationType? Type { get; set; }
    public NotificationPriority? Priority { get; set; }
    public string? Search { get; set; }
    public DateTimeOffset? FromDate { get; set; }
    public DateTimeOffset? ToDate { get; set; }
    public bool? IsRead { get; set; }
}


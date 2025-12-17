using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using LogsDtoCloneTest.Domain.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace LogsDtoCloneTest.WebSite.Areas.Admin.Models;

public sealed class CreateNotificationViewModel
{
    [Display(Name = "عنوان")]
    [Required(ErrorMessage = "عنوان الزامی است.")]
    [MaxLength(500, ErrorMessage = "عنوان نباید بیش از ۵۰۰ کاراکتر باشد.")]
    public string Title { get; set; } = string.Empty;

    [Display(Name = "متن اعلان")]
    [Required(ErrorMessage = "متن اعلان الزامی است.")]
    [MaxLength(5000, ErrorMessage = "متن اعلان نباید بیش از ۵۰۰۰ کاراکتر باشد.")]
    public string Message { get; set; } = string.Empty;

    [Display(Name = "نوع اعلان")]
    [Required(ErrorMessage = "نوع اعلان الزامی است.")]
    public NotificationType Type { get; set; } = NotificationType.General;

    [Display(Name = "اولویت")]
    [Required(ErrorMessage = "اولویت الزامی است.")]
    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;

    [Display(Name = "تاریخ انقضا (شمسی)")]
    public string? ExpiresAtPersian { get; set; }

    [Display(Name = "تاریخ انقضا")]
    public DateTime? ExpiresAt { get; set; }

    [Display(Name = "نقش‌ها")]
    public IReadOnlyCollection<string>? SelectedRoles { get; set; }

    [Display(Name = "تاریخ عضویت از (شمسی)")]
    public string? RegisteredFromPersian { get; set; }

    [Display(Name = "تاریخ عضویت تا (شمسی)")]
    public string? RegisteredToPersian { get; set; }

    [Display(Name = "کاربران انتخابی")]
    public IReadOnlyCollection<string>? SelectedUserIds { get; set; }

    public IReadOnlyCollection<SelectListItem> AvailableRoles { get; set; } = Array.Empty<SelectListItem>();
    public IReadOnlyCollection<SelectListItem> TypeOptions { get; set; } = Array.Empty<SelectListItem>();
    public IReadOnlyCollection<SelectListItem> PriorityOptions { get; set; } = Array.Empty<SelectListItem>();
    public IReadOnlyCollection<SelectListItem> AvailableUsers { get; set; } = Array.Empty<SelectListItem>();
}

public sealed class NotificationListViewModel
{
    public IReadOnlyCollection<NotificationItemViewModel> Items { get; set; } = Array.Empty<NotificationItemViewModel>();
    public int TotalCount { get; set; }
    public int UnreadCount { get; set; }
    public bool? IsReadFilter { get; set; }
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

public sealed class AdminNotificationListViewModel
{
    public IReadOnlyCollection<AdminNotificationItemViewModel> Items { get; set; } = Array.Empty<AdminNotificationItemViewModel>();
    public AdminNotificationStatsViewModel Stats { get; set; } = new();
    public string? SearchTitle { get; set; }
    public string? SearchMessage { get; set; }
    public int? TypeFilter { get; set; }
    public int? PriorityFilter { get; set; }
    public string? DateFromFilterPersian { get; set; }
    public string? DateToFilterPersian { get; set; }
    public bool? IsActiveFilter { get; set; }
}

public sealed class AdminNotificationItemViewModel
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public NotificationPriority Priority { get; set; }
    public DateTimeOffset SentAt { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public bool IsActive { get; set; }
    public int RecipientCount { get; set; }
    public bool IsExpired { get; set; }
}

public sealed class AdminNotificationStatsViewModel
{
    public int Total { get; set; }
    public int Active { get; set; }
    public int Inactive { get; set; }
    public int Expired { get; set; }
}

public sealed class AdminEditNotificationViewModel
{
    [Display(Name = "عنوان")]
    [Required(ErrorMessage = "عنوان الزامی است.")]
    [MaxLength(500, ErrorMessage = "عنوان نباید بیش از ۵۰۰ کاراکتر باشد.")]
    public string Title { get; set; } = string.Empty;

    [Display(Name = "متن اعلان")]
    [Required(ErrorMessage = "متن اعلان الزامی است.")]
    [MaxLength(5000, ErrorMessage = "متن اعلان نباید بیش از ۵۰۰۰ کاراکتر باشد.")]
    public string Message { get; set; } = string.Empty;

    [Display(Name = "تاریخ انقضا (شمسی)")]
    public string? ExpiresAtPersian { get; set; }

    [Display(Name = "تاریخ انقضا")]
    public DateTime? ExpiresAt { get; set; }
}



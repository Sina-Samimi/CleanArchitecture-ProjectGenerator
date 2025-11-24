using System.ComponentModel.DataAnnotations;

namespace Arsis.Domain.Enums;

public enum InvoiceStatus
{
    [Display(Name = "پیش‌نویس")] Draft = 0,
    [Display(Name = "در انتظار پرداخت")] Pending = 1,
    [Display(Name = "تسویه شده")] Paid = 2,
    [Display(Name = "تسویه جزئی")] PartiallyPaid = 3,
    [Display(Name = "لغو شده")] Cancelled = 4,
    [Display(Name = "سررسید گذشته")] Overdue = 5
}

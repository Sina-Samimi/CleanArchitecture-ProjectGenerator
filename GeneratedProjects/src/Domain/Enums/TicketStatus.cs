using System.ComponentModel.DataAnnotations;

namespace TestAttarClone.Domain.Enums;

public enum TicketStatus
{
    [Display(Name = "در انتظار بررسی")]
    Pending = 1,

    [Display(Name = "در حال بررسی")]
    InProgress = 2,

    [Display(Name = "پاسخ داده شده")]
    Answered = 3,

    [Display(Name = "بسته شده")]
    Closed = 4
}

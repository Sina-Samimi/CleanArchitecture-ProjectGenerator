using System.ComponentModel.DataAnnotations;

namespace LogTableRenameTest.Domain.Enums;

public enum WithdrawalRequestStatus
{
    [Display(Name = "در انتظار بررسی")]
    Pending = 0,

    [Display(Name = "تایید شده")]
    Approved = 1,

    [Display(Name = "پرداخت شده")]
    Processed = 2,

    [Display(Name = "رد شده")]
    Rejected = 3,

    [Display(Name = "لغو شده")]
    Cancelled = 4
}


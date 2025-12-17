using System.ComponentModel.DataAnnotations;

namespace LogTableRenameTest.Domain.Enums;

public enum TransactionStatus
{
    [Display(Name = "در انتظار")] Pending = 0,
    [Display(Name = "موفق")] Succeeded = 1,
    [Display(Name = "ناموفق")] Failed = 2,
    [Display(Name = "لغو شده")] Cancelled = 3,
    [Display(Name = "عودت داده شده")] Refunded = 4
}

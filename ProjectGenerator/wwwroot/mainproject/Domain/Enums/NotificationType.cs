using System.ComponentModel.DataAnnotations;

namespace Attar.Domain.Enums;

public enum NotificationType
{
    [Display(Name = "اطلاعیه عمومی")]
    General = 1,

    [Display(Name = "اطلاعیه مهم")]
    Important = 2,

    [Display(Name = "هشدار")]
    Warning = 3,

    [Display(Name = "تبلیغات")]
    Promotion = 4,

    [Display(Name = "سیستم")]
    System = 5
}


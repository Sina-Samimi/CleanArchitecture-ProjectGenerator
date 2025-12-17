using System.ComponentModel.DataAnnotations;

namespace Attar.Domain.Enums;

public enum NotificationPriority
{
    [Display(Name = "پایین")]
    Low = 1,

    [Display(Name = "عادی")]
    Normal = 2,

    [Display(Name = "بالا")]
    High = 3,

    [Display(Name = "فوری")]
    Urgent = 4
}


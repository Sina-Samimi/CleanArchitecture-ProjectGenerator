using System.ComponentModel.DataAnnotations;

namespace TestAttarClone.Domain.Enums;

public enum ShipmentStatus
{
    [Display(Name = "در حال آماده سازی")]
    Preparing = 1,

    [Display(Name = "تحویل به پست")]
    DeliveredToPost = 2,

    [Display(Name = "ارسال شده")]
    Shipped = 3,

    [Display(Name = "دریافت شده توسط مشتری")]
    Delivered = 4
}


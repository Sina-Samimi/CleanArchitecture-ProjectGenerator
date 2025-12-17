using System.ComponentModel.DataAnnotations;

namespace TestAttarClone.Domain.Enums;

public enum InvoiceItemType
{
    [Display(Name = "محصول")]
    Product = 0,

    [Display(Name = "دوره آموزشی")]
    Course = 1,

    [Display(Name = "آزمون")]
    Test = 2,

    [Display(Name = "خدمت")]
    Service = 3,

    [Display(Name = "سایر")]
    Miscellaneous = 4
}

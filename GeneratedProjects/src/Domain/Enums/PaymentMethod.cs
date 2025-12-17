using System.ComponentModel.DataAnnotations;

namespace TestAttarClone.Domain.Enums;

public enum PaymentMethod
{
    [Display(Name = "نامشخص")]
    Unknown = 0,

    [Display(Name = "درگاه آنلاین")]
    OnlineGateway = 1,

    [Display(Name = "کارت به کارت / حواله")]
    BankTransfer = 2,

    [Display(Name = "نقدی")]
    Cash = 3,

    [Display(Name = "کیف پول")]
    Wallet = 4
}

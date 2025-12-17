using System.ComponentModel.DataAnnotations;

namespace TestAttarClone.Domain.Enums;

public enum WalletTransactionType
{
    [Display(Name = "واریز")]
    Credit = 1,

    [Display(Name = "برداشت")]
    Debit = 2
}

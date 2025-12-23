using System.ComponentModel.DataAnnotations;

namespace MobiRooz.Domain.Enums;

public enum WalletTransactionType
{
    [Display(Name = "واریز")]
    Credit = 1,

    [Display(Name = "برداشت")]
    Debit = 2
}

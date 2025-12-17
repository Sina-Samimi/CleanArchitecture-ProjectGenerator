using System.ComponentModel.DataAnnotations;

namespace Attar.Domain.Enums;

public enum WalletTransactionType
{
    [Display(Name = "واریز")]
    Credit = 1,

    [Display(Name = "برداشت")]
    Debit = 2
}

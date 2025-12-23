using System.ComponentModel.DataAnnotations;

namespace MobiRooz.Domain.Enums;

public enum WithdrawalRequestType
{
    [Display(Name = "برداشت سهم فروشنده")]
    SellerRevenue = 0,
    
    [Display(Name = "برداشت از کیف پول")]
    Wallet = 1
}


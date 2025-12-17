using System.ComponentModel.DataAnnotations;

namespace LogTableRenameTest.Domain.Enums;

/// <summary>
/// روش محاسبه کارمزد پلتفرم
/// </summary>
public enum PlatformCommissionCalculationMethod
{
    /// <summary>
    /// کارمزد از سهم فروشنده کسر می‌شود
    /// مبلغ واریزی = (مبلغ کل × سهم فروشنده) - (مبلغ کل × کارمزد پلتفرم)
    /// </summary>
    [Display(Name = "کسر از سهم فروشنده")]
    DeductFromSeller = 1,

    /// <summary>
    /// سهم فروشنده و کارمزد پلتفرم مکمل هم هستند
    /// مبلغ واریزی = مبلغ کل × سهم فروشنده
    /// (کارمزد پلتفرم = مبلغ کل - مبلغ واریزی)
    /// </summary>
    [Display(Name = "مکمل سهم فروشنده")]
    Complementary = 2
}


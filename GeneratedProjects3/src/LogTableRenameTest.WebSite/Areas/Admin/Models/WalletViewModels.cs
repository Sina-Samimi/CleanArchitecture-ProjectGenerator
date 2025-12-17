using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using LogTableRenameTest.Domain.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace LogTableRenameTest.WebSite.Areas.Admin.Models;

public sealed class WalletChargeFormViewModel
{
    [Required(ErrorMessage = "انتخاب کاربر الزامی است.")]
    [Display(Name = "کاربر")]
    public string? UserId { get; set; }

    [Required(ErrorMessage = "مبلغ شارژ را وارد کنید.")]
    [Range(1, double.MaxValue, ErrorMessage = "مبلغ شارژ باید بزرگ‌تر از صفر باشد.")]
    [Display(Name = "مبلغ شارژ")]
    public decimal Amount { get; set; }

    [Display(Name = "واحد پول")]
    [Required(ErrorMessage = "واحد پول را مشخص کنید.")]
    public string Currency { get; set; } = "IRT";

    [Display(Name = "عنوان فاکتور")]
    [Required(ErrorMessage = "عنوان فاکتور الزامی است.")]
    public string InvoiceTitle { get; set; } = "شارژ کیف پول";

    [Display(Name = "شرح فاکتور")]
    [DataType(DataType.MultilineText)]
    public string? InvoiceDescription { get; set; }

    [Display(Name = "توضیح تراکنش")]
    [DataType(DataType.MultilineText)]
    public string? TransactionDescription { get; set; }

    [Display(Name = "شناسه مرجع پرداخت")]
    public string? PaymentReference { get; set; }

    [Display(Name = "روش پرداخت")]
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;

    public IReadOnlyCollection<SelectListItem> UserOptions { get; set; } = Array.Empty<SelectListItem>();

    public IReadOnlyCollection<SelectListItem> PaymentMethodOptions { get; set; } = Array.Empty<SelectListItem>();
}

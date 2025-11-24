using System;
using System.ComponentModel.DataAnnotations;

namespace EndPoint.WebSite.Models.Account;

public sealed class PhoneLoginViewModel
{
    [Display(Name = "شماره موبایل")]
    [Required(ErrorMessage = "وارد کردن شماره موبایل الزامی است.")]
    public string PhoneNumber { get; set; } = string.Empty;

    public string? NormalizedPhoneNumber { get; set; }

    [Display(Name = "کد تایید")]
    public string? VerificationCode { get; set; }

    [Display(Name = "پذیرش قوانین و مقررات")]
    [MustBeTrue(ErrorMessage = "پذیرش قوانین و مقررات الزامی است.")]
    public bool AcceptTerms { get; set; }

    public bool CodeSent { get; set; }

    public DateTimeOffset? CodeExpiresAt { get; set; }

    public string? ReturnUrl { get; set; }

    public string? InfoMessage { get; set; }

    public string? ErrorMessage { get; set; }

    public bool AllowResend => CodeSent && (!CodeExpiresAt.HasValue || CodeExpiresAt <= DateTimeOffset.UtcNow);
}

using System;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using TestAttarClone.SharedKernel.Helpers;

namespace TestAttarClone.WebSite.Models.Account;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public sealed class IranianMobilePhoneAttribute : ValidationAttribute
{
    public IranianMobilePhoneAttribute()
        : base("شماره موبایل باید یک شماره موبایل معتبر ایرانی باشد (مثال: 09123456789)")
    {
    }

    public override bool IsValid(object? value)
    {
        if (value is null)
        {
            return true; // Let Required attribute handle null values
        }

        if (value is not string phoneNumber)
        {
            return false;
        }

        return PhoneNumberHelper.IsValid(phoneNumber);
    }
}


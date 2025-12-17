using System;
using System.ComponentModel.DataAnnotations;

namespace Attar.WebSite.Models.Account;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public sealed class MustBeTrueAttribute : ValidationAttribute
{
    public MustBeTrueAttribute()
        : base("The {0} field must be checked.")
    {
    }

    public override bool IsValid(object? value)
    {
        return value is bool boolValue && boolValue;
    }
}

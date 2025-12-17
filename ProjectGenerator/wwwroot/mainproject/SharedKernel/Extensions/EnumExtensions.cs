using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;

namespace Attar.SharedKernel.Extensions;

public static class EnumExtensions
{
    public static string GetDisplayName(this Enum value)
    {
        if (value is null)
        {
            return string.Empty;
        }

        var member = value.GetType()
            .GetMember(value.ToString())
            .FirstOrDefault();

        if (member is null)
        {
            return value.ToString();
        }

        var attribute = member.GetCustomAttribute<DisplayAttribute>();
        if (attribute is null)
        {
            return value.ToString();
        }

        var name = attribute.GetName();
        return string.IsNullOrWhiteSpace(name) ? value.ToString() : name!;
    }
}

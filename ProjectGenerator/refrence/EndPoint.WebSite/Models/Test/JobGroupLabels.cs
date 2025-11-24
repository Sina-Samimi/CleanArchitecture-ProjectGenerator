using System;
using System.Collections.Generic;

namespace EndPoint.WebSite.Models.Test;

public static class JobGroupLabels
{
    public static readonly IReadOnlyDictionary<string, string> Map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["G1"] = "مدیریت و فرماندهی",
        ["G2"] = "کارآفرینی",
        ["G3"] = "آموزش و تربیتی",
        ["G4"] = "امور حقوقی و اداری",
        ["G5"] = "خدماتی",
        ["G6"] = "صنعتی و فنی",
        ["G7"] = "فناوری اطلاعات و رایانه",
        ["G8"] = "سلامت و روان",
        ["G9"] = "بازرگانی، مالی و امور",
        ["G10"] = "تولیدی و صنایع دستی",
        ["G11"] = "فرهنگ و هنر",
        ["G12"] = "مشاغل تجاری و فروش"
    };

    public static string Resolve(string jobGroupCode) =>
        Map.TryGetValue(jobGroupCode, out var label)
            ? label
            : jobGroupCode;
}

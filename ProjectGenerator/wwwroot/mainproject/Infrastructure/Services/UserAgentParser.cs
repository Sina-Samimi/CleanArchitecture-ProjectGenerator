using System;
using System.Text.RegularExpressions;

namespace MobiRooz.Infrastructure.Services;

public sealed class ParsedUserAgent
{
    public string DeviceType { get; set; } = "Unknown";
    public string OperatingSystem { get; set; } = "Unknown";
    public string? OsVersion { get; set; }
    public string Browser { get; set; } = "Unknown";
    public string? BrowserVersion { get; set; }
    public string? Engine { get; set; }
    public string? RawUserAgent { get; set; }
}

public static class UserAgentParser
{
    public static ParsedUserAgent Parse(string? userAgent)
    {
        var result = new ParsedUserAgent
        {
            RawUserAgent = userAgent
        };

        if (string.IsNullOrWhiteSpace(userAgent))
        {
            return result;
        }

        var ua = userAgent.Trim();

        // Detect Device Type
        result.DeviceType = DetectDeviceType(ua);

        // Detect Operating System
        var osInfo = DetectOperatingSystem(ua);
        result.OperatingSystem = osInfo.Name;
        result.OsVersion = osInfo.Version;

        // Detect Browser and Engine
        var browserInfo = DetectBrowser(ua);
        result.Browser = browserInfo.Name;
        result.BrowserVersion = browserInfo.Version;
        result.Engine = browserInfo.Engine;

        return result;
    }

    private static string DetectDeviceType(string userAgent)
    {
        if (userAgent.Contains("iPad", StringComparison.OrdinalIgnoreCase) ||
            userAgent.Contains("Tablet", StringComparison.OrdinalIgnoreCase))
        {
            return "Tablet";
        }

        if (userAgent.Contains("Mobile", StringComparison.OrdinalIgnoreCase) ||
            userAgent.Contains("Android", StringComparison.OrdinalIgnoreCase) ||
            userAgent.Contains("iPhone", StringComparison.OrdinalIgnoreCase) ||
            userAgent.Contains("iPod", StringComparison.OrdinalIgnoreCase))
        {
            return "Mobile";
        }

        return "Desktop";
    }

    private static (string Name, string? Version) DetectOperatingSystem(string userAgent)
    {
        // Windows
        var windowsMatch = Regex.Match(userAgent, @"Windows NT (\d+\.\d+)", RegexOptions.IgnoreCase);
        if (windowsMatch.Success)
        {
            var version = windowsMatch.Groups[1].Value;
            var osName = version switch
            {
                "10.0" => "Windows 10/11",
                "6.3" => "Windows 8.1",
                "6.2" => "Windows 8",
                "6.1" => "Windows 7",
                _ => "Windows"
            };
            return (osName, version);
        }

        // macOS
        var macMatch = Regex.Match(userAgent, @"Mac OS X (\d+[._]\d+(?:[._]\d+)?)", RegexOptions.IgnoreCase);
        if (macMatch.Success)
        {
            return ("macOS", macMatch.Groups[1].Value.Replace('_', '.'));
        }

        // Linux
        if (userAgent.Contains("Linux", StringComparison.OrdinalIgnoreCase) &&
            !userAgent.Contains("Android", StringComparison.OrdinalIgnoreCase))
        {
            var linuxDistro = "Linux";
            if (userAgent.Contains("Ubuntu", StringComparison.OrdinalIgnoreCase))
                linuxDistro = "Ubuntu";
            else if (userAgent.Contains("Fedora", StringComparison.OrdinalIgnoreCase))
                linuxDistro = "Fedora";
            else if (userAgent.Contains("Debian", StringComparison.OrdinalIgnoreCase))
                linuxDistro = "Debian";
            return (linuxDistro, null);
        }

        // Android
        var androidMatch = Regex.Match(userAgent, @"Android (\d+(?:\.\d+)?)", RegexOptions.IgnoreCase);
        if (androidMatch.Success)
        {
            return ("Android", androidMatch.Groups[1].Value);
        }

        // iOS
        var iosMatch = Regex.Match(userAgent, @"OS (\d+[._]\d+(?:[._]\d+)?)", RegexOptions.IgnoreCase);
        if (iosMatch.Success)
        {
            return ("iOS", iosMatch.Groups[1].Value.Replace('_', '.'));
        }

        // Chrome OS
        if (userAgent.Contains("CrOS", StringComparison.OrdinalIgnoreCase))
        {
            return ("Chrome OS", null);
        }

        return ("Unknown", null);
    }

    private static (string Name, string? Version, string? Engine) DetectBrowser(string userAgent)
    {
        // Edge (Chromium)
        var edgeMatch = Regex.Match(userAgent, @"Edg/(\d+(?:\.\d+)?)", RegexOptions.IgnoreCase);
        if (edgeMatch.Success)
        {
            return ("Microsoft Edge", edgeMatch.Groups[1].Value, "Blink");
        }

        // Edge (Legacy)
        if (userAgent.Contains("Edge/", StringComparison.OrdinalIgnoreCase))
        {
            var edgeLegacyMatch = Regex.Match(userAgent, @"Edge/(\d+(?:\.\d+)?)", RegexOptions.IgnoreCase);
            return ("Microsoft Edge (Legacy)", edgeLegacyMatch.Success ? edgeLegacyMatch.Groups[1].Value : null, "EdgeHTML");
        }

        // Opera
        var operaMatch = Regex.Match(userAgent, @"OPR/(\d+(?:\.\d+)?)", RegexOptions.IgnoreCase);
        if (operaMatch.Success)
        {
            return ("Opera", operaMatch.Groups[1].Value, "Blink");
        }

        if (userAgent.Contains("Opera", StringComparison.OrdinalIgnoreCase))
        {
            var operaVersionMatch = Regex.Match(userAgent, @"Version/(\d+(?:\.\d+)?)", RegexOptions.IgnoreCase);
            return ("Opera", operaVersionMatch.Success ? operaVersionMatch.Groups[1].Value : null, "Presto");
        }

        // Chrome
        var chromeMatch = Regex.Match(userAgent, @"Chrome/(\d+(?:\.\d+)?)", RegexOptions.IgnoreCase);
        if (chromeMatch.Success && !userAgent.Contains("Edg", StringComparison.OrdinalIgnoreCase))
        {
            return ("Chrome", chromeMatch.Groups[1].Value, "Blink");
        }

        // Firefox
        var firefoxMatch = Regex.Match(userAgent, @"Firefox/(\d+(?:\.\d+)?)", RegexOptions.IgnoreCase);
        if (firefoxMatch.Success)
        {
            return ("Firefox", firefoxMatch.Groups[1].Value, "Gecko");
        }

        // Safari
        if (userAgent.Contains("Safari", StringComparison.OrdinalIgnoreCase) &&
            !userAgent.Contains("Chrome", StringComparison.OrdinalIgnoreCase))
        {
            var safariVersionMatch = Regex.Match(userAgent, @"Version/(\d+(?:\.\d+)?)", RegexOptions.IgnoreCase);
            return ("Safari", safariVersionMatch.Success ? safariVersionMatch.Groups[1].Value : null, "WebKit");
        }

        // Internet Explorer
        var ieMatch = Regex.Match(userAgent, @"MSIE (\d+(?:\.\d+)?)", RegexOptions.IgnoreCase);
        if (ieMatch.Success)
        {
            return ("Internet Explorer", ieMatch.Groups[1].Value, "Trident");
        }

        var tridentMatch = Regex.Match(userAgent, @"Trident/(\d+(?:\.\d+)?)", RegexOptions.IgnoreCase);
        if (tridentMatch.Success)
        {
            var rvMatch = Regex.Match(userAgent, @"rv:(\d+(?:\.\d+)?)", RegexOptions.IgnoreCase);
            return ("Internet Explorer", rvMatch.Success ? rvMatch.Groups[1].Value : null, "Trident");
        }

        return ("Unknown", null, null);
    }
}


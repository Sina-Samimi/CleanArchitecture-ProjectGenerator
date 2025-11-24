using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace EndPoint.WebSite.App;

public interface IJobGroupLabelsProvider
{
    Task<Dictionary<string, string>> LoadAsync(CancellationToken cancellationToken = default);
}

public sealed class JobGroupLabelsProvider : IJobGroupLabelsProvider
{
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<JobGroupLabelsProvider> _logger;

    private static readonly string[] Codes =
        Enumerable.Range(1, 12).Select(i => $"G{i}").ToArray();

    private static readonly Dictionary<string, string> Fallback = new(StringComparer.OrdinalIgnoreCase)
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

    public JobGroupLabelsProvider(IWebHostEnvironment environment, ILogger<JobGroupLabelsProvider> logger)
    {
        _environment = environment;
        _logger = logger;
    }

    public async Task<Dictionary<string, string>> LoadAsync(CancellationToken cancellationToken = default)
    {
        var root = _environment.ContentRootPath;

        var jsonPath = Path.Combine(root, "data", "job_groups.json");
        var fromJson = await TryLoadJsonAsync(jsonPath, cancellationToken).ConfigureAwait(false);
        if (Complete(fromJson))
        {
            _logger.LogInformation("Loaded job group labels from {File}", jsonPath);
            return fromJson;
        }

        var csvPath = Path.Combine(root, "data", "job_groups.csv");
        var fromCsv = await TryLoadSimpleCsvAsync(csvPath, cancellationToken).ConfigureAwait(false);
        if (Complete(fromCsv))
        {
            _logger.LogInformation("Loaded job group labels from {File}", csvPath);
            return fromCsv;
        }

        var jobSkillPath = Path.Combine(root, "matrices", "job_skill.csv");
        var fromJobSkill = await TryLoadJobSkillAsync(jobSkillPath, cancellationToken).ConfigureAwait(false);
        if (Complete(fromJobSkill))
        {
            _logger.LogInformation("Loaded job group labels from {File}", jobSkillPath);
            return fromJobSkill;
        }

        var talentJobPath = Path.Combine(root, "matrices", "talent_job.csv");
        var fromTalentJob = await TryInferFromTalentJobAsync(talentJobPath, cancellationToken).ConfigureAwait(false);
        if (Complete(fromTalentJob, requireNonGeneric: true))
        {
            _logger.LogInformation("Inferred job group labels from {File}", talentJobPath);
            return fromTalentJob;
        }

        _logger.LogWarning("Job group labels not found in matrices; using fallback labels.");
        return new Dictionary<string, string>(Fallback, StringComparer.OrdinalIgnoreCase);
    }

    private static async Task<Dictionary<string, string>> TryLoadJsonAsync(string path, CancellationToken cancellationToken)
    {
        if (!File.Exists(path))
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        try
        {
            await using var stream = File.OpenRead(path);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var data = await JsonSerializer.DeserializeAsync<List<JobGroupJson>>(stream, options, cancellationToken)
                       ?? new List<JobGroupJson>();
            return ToMap(data.Select(x => (x.Code, x.Label)));
        }
        catch
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private static async Task<Dictionary<string, string>> TryLoadSimpleCsvAsync(string path, CancellationToken cancellationToken)
    {
        if (!File.Exists(path))
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        try
        {
            var lines = await File.ReadAllLinesAsync(path, cancellationToken).ConfigureAwait(false);
            if (lines.Length <= 1)
            {
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }

            var records = lines
                .Skip(1)
                .Select(line =>
                {
                    var parts = line.Split(',', StringSplitOptions.TrimEntries);
                    return parts.Length >= 2 ? (parts[0], parts[1]) : (null, null);
                });

            return ToMap(records!);
        }
        catch
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private static async Task<Dictionary<string, string>> TryLoadJobSkillAsync(string path, CancellationToken cancellationToken)
    {
        if (!File.Exists(path))
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        try
        {
            var lines = await File.ReadAllLinesAsync(path, cancellationToken).ConfigureAwait(false);
            if (lines.Length == 0)
            {
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }

            var header = lines[0];
            var columns = header.Split(',', StringSplitOptions.TrimEntries)
                .Select(c => c.ToLowerInvariant())
                .ToArray();

            var jobIndex = Array.FindIndex(columns, c => c is "jobgroup" or "group");
            var labelIndex = Array.FindIndex(columns, c => c is "label" or "name_fa" or "fa");

            if (jobIndex < 0 || labelIndex < 0)
            {
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }

            var pairs = lines
                .Skip(1)
                .Select(line =>
                {
                    var parts = line.Split(',', StringSplitOptions.TrimEntries);
                    if (parts.Length <= Math.Max(jobIndex, labelIndex))
                    {
                        return (null, null);
                    }

                    return (parts[jobIndex], parts[labelIndex]);
                });

            return ToMap(pairs!);
        }
        catch
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private static async Task<Dictionary<string, string>> TryInferFromTalentJobAsync(string path, CancellationToken cancellationToken)
    {
        if (!File.Exists(path))
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        try
        {
            var lines = await File.ReadAllLinesAsync(path, cancellationToken).ConfigureAwait(false);
            if (lines.Length == 0)
            {
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }

            var headerTokens = lines[0].Split(',', StringSplitOptions.TrimEntries);
            if (headerTokens.Length < 13)
            {
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }

            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (var i = 1; i <= 12; i++)
            {
                var token = headerTokens.Length > i ? headerTokens[i] : string.Empty;
                map[$"G{i}"] = NeedsFallback(token) ? $"G{i}" : token;
            }

            return map;
        }
        catch
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        static bool NeedsFallback(string? token) =>
            string.IsNullOrWhiteSpace(token) ||
            token.StartsWith("G", StringComparison.OrdinalIgnoreCase) ||
            int.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out _);
    }

    private static Dictionary<string, string> ToMap(IEnumerable<(string? code, string? label)> pairs)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var (code, label) in pairs)
        {
            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(label))
            {
                continue;
            }

            map[code.Trim()] = label.Trim();
        }

        return map;
    }

    private static bool Complete(Dictionary<string, string> map, bool requireNonGeneric = false)
    {
        if (map.Count == 0)
        {
            return false;
        }

        foreach (var code in Codes)
        {
            if (!map.TryGetValue(code, out var label) || string.IsNullOrWhiteSpace(label))
            {
                return false;
            }

            if (requireNonGeneric && label.StartsWith("G", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return true;
    }

    private sealed record JobGroupJson(string Code, string Label);
}

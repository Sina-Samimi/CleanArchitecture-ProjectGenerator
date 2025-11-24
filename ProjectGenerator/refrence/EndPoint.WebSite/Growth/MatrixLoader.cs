using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Configuration;

namespace EndPoint.WebSite.Growth;

public static class MatrixLoader
{
    public static CorrelationMatrices Load(IConfiguration configuration, string contentRoot)
    {
        var section = configuration.GetSection("Matrices");
        if (!section.Exists())
        {
            throw new InvalidOperationException("بخش Matrices در فایل پیکربندی یافت نشد.");
        }

        string ResolvePath(string key)
        {
            var relative = section[key];
            if (string.IsNullOrWhiteSpace(relative))
            {
                throw new InvalidOperationException($"مسیر ماتریس '{key}' در پیکربندی مشخص نشده است.");
            }

            return Path.IsPathRooted(relative)
                ? relative
                : Path.Combine(contentRoot, relative);
        }

        var talentJobPath = ResolvePath("TalentJob");
        var valueJobPath = ResolvePath("ValueJob");
        var talentSkillPath = ResolvePath("TalentSkill");
        var jobSkillPath = ResolvePath("JobSkill");
        var skillLevelPath = ResolvePath("SkillLevels");

        var talentJob = ReadMatrix(talentJobPath, out var talentIds, out var jobIds); // 34 x 12
        var valueJob = ReadMatrix(valueJobPath, out var valueIds, out var jobIdsFromValue); // 10 x 12
        var talentSkill = ReadMatrix(talentSkillPath, out var talentIdsFromSkill, out var skillIds); // 34 x 118
        var jobSkill = ReadMatrix(jobSkillPath, out var jobIdsFromSkill, out var skillIdsFromJob); // 12 x 118

        ValidateIdentity(talentIds, talentIdsFromSkill, "شناسه‌های استعداد (Talent)");
        ValidateIdentity(jobIds, jobIdsFromValue, "شناسه‌های گروه شغلی");
        ValidateIdentity(skillIds, skillIdsFromJob, "شناسه‌های مهارت");

        var skillLevels = File.ReadAllLines(skillLevelPath)
            .Skip(1)
            .Select(line => line.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            .Where(parts => parts.Length >= 2)
            .ToDictionary(parts => parts[0], parts => int.Parse(parts[1]), StringComparer.OrdinalIgnoreCase);

        return new CorrelationMatrices
        {
            TalentIds = talentIds,
            ValueIds = valueIds,
            JobIds = jobIds,
            SkillIds = skillIds,
            TalentJob = talentJob,
            ValueJob = valueJob,
            TalentSkill = talentSkill,
            JobSkill = jobSkill,
            SkillLevelMap = skillLevels
        };
    }

    private static double[,] ReadMatrix(string path, out List<string> rowIds, out List<string> columnIds)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"فایل ماتریس یافت نشد: {path}");
        }

        var lines = File.ReadAllLines(path);
        if (lines.Length <= 1)
        {
            throw new InvalidOperationException($"فایل {path} داده‌ای برای بارگذاری ندارد.");
        }

        var headers = lines[0].Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToList();
        if (headers.Count < 2)
        {
            throw new InvalidOperationException($"هدر فایل {path} نامعتبر است.");
        }

        columnIds = headers.Skip(1).ToList();
        rowIds = new List<string>(lines.Length - 1);
        var matrix = new double[lines.Length - 1, columnIds.Count];

        for (var i = 1; i < lines.Length; i++)
        {
            var parts = lines[i].Split(',', StringSplitOptions.TrimEntries);
            if (parts.Length == 0 || string.IsNullOrWhiteSpace(parts[0]))
            {
                continue;
            }

            rowIds.Add(parts[0]);
            for (var j = 1; j < parts.Length && j <= columnIds.Count; j++)
            {
                matrix[rowIds.Count - 1, j - 1] = double.Parse(parts[j], CultureInfo.InvariantCulture);
            }
        }

        return matrix;
    }

    private static void ValidateIdentity(IReadOnlyList<string> primary, IReadOnlyList<string> secondary, string label)
    {
        if (primary.Count != secondary.Count)
        {
            throw new InvalidOperationException($"تعداد عناصر {label} در فایل‌های ماتریس با هم سازگار نیست.");
        }

        for (var i = 0; i < primary.Count; i++)
        {
            if (!string.Equals(primary[i], secondary[i], StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"ترتیب {label} در فایل‌های ماتریس سازگار نیست. مقدار '{primary[i]}' با '{secondary[i]}' مطابقت ندارد.");
            }
        }
    }
}

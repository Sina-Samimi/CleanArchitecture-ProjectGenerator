using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace EndPoint.WebSite.Growth;

public static class ResultPdfBuilder
{
    private static readonly object Sync = new();
    private static bool _questPdfInitialized;
    private static bool _fontRegistered;
    private static string? _fontFamilyName;

    public static IDocument Build(
        AssessmentResultDto result,
        IReadOnlyDictionary<string, string> labels,
        string fontPath)
    {
        EnsureQuestPdf(fontPath);

        var culture = CultureInfo.GetCultureInfo("fa-IR");
        var pvqScores = result.PvqScores.OrderByDescending(x => x.Value).ToList();
        var cliftonScores = result.CliftonScores.OrderByDescending(x => x.Value).ToList();
        var jobScores = result.JobScores.OrderByDescending(x => x.Score).ToList();
        var topPlans = result.SkillPlans.Take(3).ToList();

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(30);
                page.Size(PageSizes.A4);
                page.PageColor(Colors.White);

                page.DefaultTextStyle(style =>
                {
                    var textStyle = style
                        .FontSize(11)
                        .DirectionFromRightToLeft();

                    if (!string.IsNullOrWhiteSpace(_fontFamilyName))
                    {
                        textStyle = textStyle.FontFamily(_fontFamilyName);
                    }

                    return textStyle;
                });

                page.Content().Column(column =>
                {
                    column.Spacing(18);

                    column.Item().Text("گزارش ارزیابی Clifton + Schwartz")
                        .SemiBold()
                        .FontSize(18)
                        .AlignCenter();

                    column.Item().Text(text =>
                    {
                        text.Span("تاریخ تولید: ").SemiBold();
                        text.Span(DateTimeOffset.Now.ToString("yyyy/MM/dd HH:mm", culture));
                    });

                    if (pvqScores.Count > 0)
                    {
                        column.Item().Component(new ScoreTableComponent(
                            "ارزش‌های PVQ",
                            "ارزش",
                            "امتیاز",
                            pvqScores.Select(x => (Label: x.Key, Code: string.Empty, Score: x.Value)),
                            culture));
                    }

                    if (cliftonScores.Count > 0)
                    {
                        column.Item().Component(new ScoreTableComponent(
                            "استعدادهای Clifton",
                            "استعداد",
                            "امتیاز",
                            cliftonScores.Select(x => (Label: x.Key, Code: string.Empty, Score: x.Value)),
                            culture));
                    }

                    if (jobScores.Count > 0)
                    {
                        column.Item().Component(new ScoreTableComponent(
                            "اولویت گروه‌های شغلی",
                            "گروه شغلی",
                            "امتیاز",
                            jobScores.Select(x => (Label: labels.TryGetValue(x.JobCode, out var fa) ? fa : x.JobCode, Code: string.Empty, Score: x.Score)),
                            culture));
                    }

                    if (topPlans.Count > 0)
                    {
                        column.Item().Text("برنامه مهارتی چهار سطحی (سه گروه برتر)")
                            .SemiBold()
                            .FontSize(14);

                    column.Item().Row(row =>
                        {
                            row.Spacing(12);
                            foreach (var plan in topPlans)
                            {
                                var label = labels.TryGetValue(plan.JobCode, out var fa)
                                    ? fa
                                    : plan.JobCode;

                                row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(12).Column(col =>
                                {
                                    col.Spacing(6);
                                    col.Item().Text(label).SemiBold().FontSize(12);

                                    AddSkillSection(col, "Self Awareness", plan.SelfAwareness);
                                    AddSkillSection(col, "Self Building", plan.SelfBuilding);
                                    AddSkillSection(col, "Self Development", plan.SelfDevelopment);
                                    AddSkillSection(col, "Self Actualization", plan.SelfActualization);
                                });
                            }
                        });
                    }
                });
            });
        });
    }

    private static void EnsureQuestPdf(string fontPath)
    {
        lock (Sync)
        {
            if (!_questPdfInitialized)
            {
                QuestPDF.Settings.License = LicenseType.Community;
                _questPdfInitialized = true;
            }

            if (_fontRegistered)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(fontPath))
            {
                var absolutePath = Path.IsPathRooted(fontPath)
                    ? fontPath
                    : Path.Combine(Directory.GetCurrentDirectory(), fontPath);

                if (File.Exists(absolutePath))
                {
                    using var stream = File.OpenRead(absolutePath);
                    QuestPDF.Drawing.FontManager.RegisterFont(stream);
                    _fontFamilyName = "Vazirmatn";
                    _fontRegistered = true;
                    return;
                }
            }

            _fontFamilyName = null;
            _fontRegistered = true;
        }
    }

    private static void AddSkillSection(ColumnDescriptor column, string title, IReadOnlyList<string> items)
    {
        column.Item().Text(title).SemiBold().FontSize(11);
        column.Item().Text(string.Join("، ", items)).FontSize(10).LineHeight(1.3f);
    }

    private sealed class ScoreTableComponent : IComponent
    {
        private readonly string _title;
        private readonly string _labelHeader;
        private readonly string _scoreHeader;
        private readonly IEnumerable<(string Label, string Code, double Score)> _rows;
        private readonly CultureInfo _culture;

        public ScoreTableComponent(
            string title,
            string labelHeader,
            string scoreHeader,
            IEnumerable<(string Label, string Code, double Score)> rows,
            CultureInfo culture)
        {
            _title = title;
            _labelHeader = labelHeader;
            _scoreHeader = scoreHeader;
            _rows = rows;
            _culture = culture;
        }

        public void Compose(IContainer container)
        {
            container.Border(1)
                .BorderColor(Colors.Grey.Lighten2)
                .Padding(12)
                .Column(column =>
                {
                    column.Spacing(8);
                    column.Item().Text(_title).SemiBold().FontSize(14);
                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn();
                            columns.ConstantColumn(90);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Text(_labelHeader).SemiBold();
                            header.Cell().AlignLeft().Text(_scoreHeader).SemiBold();
                        });

                        foreach (var (label, code, score) in _rows)
                        {
                            var display = string.IsNullOrWhiteSpace(code) || label.Equals(code, StringComparison.OrdinalIgnoreCase)
                                ? label
                                : $"{label} ({code})";

                            table.Cell().Text(display);

                            table.Cell().AlignLeft().Text(score.ToString("0.000", _culture));
                        }
                    });
                });
        }
    }
}

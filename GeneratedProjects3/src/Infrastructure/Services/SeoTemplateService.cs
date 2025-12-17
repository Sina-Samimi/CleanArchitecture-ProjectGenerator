using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using LogTableRenameTest.Application.DTOs.Seo;
using LogTableRenameTest.Application.Interfaces;
using LogTableRenameTest.Domain.Enums;

namespace LogTableRenameTest.Infrastructure.Services;

public sealed class SeoTemplateService : ISeoTemplateService
{
    private readonly ISeoMetadataService _seoMetadataService;

    public SeoTemplateService(ISeoMetadataService seoMetadataService)
    {
        _seoMetadataService = seoMetadataService;
    }

    public string RenderTemplate(string template, Dictionary<string, string> variables)
    {
        if (string.IsNullOrWhiteSpace(template))
        {
            return string.Empty;
        }

        var result = template;

        // جایگزینی متغیرهای ساده: {variableName}
        foreach (var variable in variables)
        {
            var placeholder = $"{{{variable.Key}}}";
            result = result.Replace(placeholder, variable.Value ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        }

        // حذف متغیرهای استفاده نشده
        result = Regex.Replace(result, @"\{[^}]+\}", string.Empty, RegexOptions.IgnoreCase);

        return result.Trim();
    }

    public string RenderRobotsTemplate(string? template, Dictionary<string, object> variables)
    {
        if (string.IsNullOrWhiteSpace(template))
        {
            return string.Empty;
        }

        // پشتیبانی از منطق شرطی ساده: {condition ? 'value1' : 'value2'}
        var result = template;

        // جایگزینی متغیرهای boolean
        foreach (var variable in variables)
        {
            var boolValue = variable.Value switch
            {
                bool b => b,
                string s when bool.TryParse(s, out var parsed) => parsed,
                int i => i != 0,
                _ => false
            };

            var placeholder = $"{{{variable.Key}}}";
            result = result.Replace(placeholder, boolValue.ToString().ToLower(), StringComparison.OrdinalIgnoreCase);
        }

        // پردازش ternary operator ساده: {isIndex ? 'index,follow' : 'noindex,follow'}
        var ternaryPattern = @"\{([^?]+)\s*\?\s*'([^']+)'\s*:\s*'([^']+)'\}";
        result = Regex.Replace(result, ternaryPattern, match =>
        {
            var condition = match.Groups[1].Value.Trim();
            var trueValue = match.Groups[2].Value;
            var falseValue = match.Groups[3].Value;

            // بررسی شرط
            var conditionResult = EvaluateCondition(condition, variables);
            return conditionResult ? trueValue : falseValue;
        }, RegexOptions.IgnoreCase);

        // حذف متغیرهای استفاده نشده
        result = Regex.Replace(result, @"\{[^}]+\}", string.Empty, RegexOptions.IgnoreCase);

        return result.Trim();
    }

    public async Task<SeoMetadataDto?> GenerateDynamicSeo(
        SeoPageType pageType,
        string? pageIdentifier,
        Dictionary<string, string> variables,
        CancellationToken cancellationToken)
    {
        var seoMetadata = await _seoMetadataService.GetSeoMetadataAsync(pageType, pageIdentifier, cancellationToken);

        if (seoMetadata is null || !seoMetadata.UseTemplate)
        {
            return seoMetadata;
        }

        // Render templates
        var renderedTitle = RenderTemplate(seoMetadata.TitleTemplate ?? string.Empty, variables);
        var renderedDescription = RenderTemplate(seoMetadata.DescriptionTemplate ?? string.Empty, variables);
        var renderedOgTitle = RenderTemplate(seoMetadata.OgTitleTemplate ?? string.Empty, variables);
        var renderedOgDescription = RenderTemplate(seoMetadata.OgDescriptionTemplate ?? string.Empty, variables);

        // ایجاد DTO جدید با مقادیر render شده
        var dynamicSeo = new SeoMetadataDto(
            seoMetadata.Id,
            seoMetadata.PageType,
            seoMetadata.PageIdentifier,
            string.IsNullOrWhiteSpace(renderedTitle) ? seoMetadata.MetaTitle : renderedTitle,
            string.IsNullOrWhiteSpace(renderedDescription) ? seoMetadata.MetaDescription : renderedDescription,
            seoMetadata.MetaKeywords,
            seoMetadata.MetaRobots, // Robots بعداً render می‌شود
            seoMetadata.CanonicalUrl,
            seoMetadata.UseTemplate,
            seoMetadata.TitleTemplate,
            seoMetadata.DescriptionTemplate,
            seoMetadata.OgTitleTemplate,
            seoMetadata.OgDescriptionTemplate,
            seoMetadata.RobotsTemplate,
            string.IsNullOrWhiteSpace(renderedOgTitle) ? seoMetadata.OgTitle : renderedOgTitle,
            string.IsNullOrWhiteSpace(renderedOgDescription) ? seoMetadata.OgDescription : renderedOgDescription,
            seoMetadata.OgImage,
            seoMetadata.OgType,
            seoMetadata.OgUrl,
            seoMetadata.TwitterCard,
            seoMetadata.TwitterTitle,
            seoMetadata.TwitterDescription,
            seoMetadata.TwitterImage,
            seoMetadata.SchemaJson,
            seoMetadata.BreadcrumbsJson,
            seoMetadata.SitemapPriority,
            seoMetadata.SitemapChangefreq,
            seoMetadata.H1Title,
            seoMetadata.FeaturedImageUrl,
            seoMetadata.FeaturedImageAlt,
            seoMetadata.Tags,
            seoMetadata.Description,
            seoMetadata.CreateDate,
            seoMetadata.UpdateDate);

        return dynamicSeo;
    }

    private static bool EvaluateCondition(string condition, Dictionary<string, object> variables)
    {
        condition = condition.Trim();

        // بررسی متغیرهای boolean
        if (variables.TryGetValue(condition, out var value))
        {
            return value switch
            {
                bool b => b,
                string s when bool.TryParse(s, out var parsed) => parsed,
                int i => i != 0,
                _ => false
            };
        }

        // بررسی conditions ساده مثل "isIndex", "hasResults"
        if (condition.Contains("!", StringComparison.OrdinalIgnoreCase))
        {
            var negatedCondition = condition.Replace("!", string.Empty, StringComparison.OrdinalIgnoreCase).Trim();
            return !EvaluateCondition(negatedCondition, variables);
        }

        // بررسی مقایسه‌های ساده
        if (condition.Contains(">", StringComparison.OrdinalIgnoreCase))
        {
            var parts = condition.Split('>');
            if (parts.Length == 2 && variables.TryGetValue(parts[0].Trim(), out var leftValue))
            {
                if (int.TryParse(parts[1].Trim(), out var rightValue) && int.TryParse(leftValue.ToString(), out var leftInt))
                {
                    return leftInt > rightValue;
                }
            }
        }

        return false;
    }
}


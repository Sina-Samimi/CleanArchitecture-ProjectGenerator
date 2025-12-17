using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Attar.Application.DTOs.Seo;
using Attar.Domain.Enums;

namespace Attar.Application.Interfaces;

public interface ISeoTemplateService
{
    /// <summary>
    /// Render یک template با متغیرها
    /// </summary>
    string RenderTemplate(string template, Dictionary<string, string> variables);

    /// <summary>
    /// تولید SEO داینامیک بر اساس template و متغیرها
    /// </summary>
    Task<SeoMetadataDto?> GenerateDynamicSeo(
        SeoPageType pageType,
        string? pageIdentifier,
        Dictionary<string, string> variables,
        CancellationToken cancellationToken);

    /// <summary>
    /// Render Robots template با منطق شرطی
    /// </summary>
    string RenderRobotsTemplate(string? template, Dictionary<string, object> variables);
}


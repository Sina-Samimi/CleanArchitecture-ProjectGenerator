using System.Collections.Generic;

namespace EndPoint.WebSite.Models.Product;

public class ProductDetailViewModel
{
    public required ProductSummaryViewModel Product { get; init; }

    public required string HeroImageUrl { get; init; }

    public required string Description { get; init; }

    public string? DifficultyLevel { get; init; }

    public string? Duration { get; init; }

    public IReadOnlyList<string> Highlights { get; init; } = System.Array.Empty<string>();

    public IReadOnlyList<ProductModuleViewModel> Modules { get; init; } = System.Array.Empty<ProductModuleViewModel>();

    public IReadOnlyList<ProductStatisticViewModel> Statistics { get; init; } = System.Array.Empty<ProductStatisticViewModel>();

    public IReadOnlyList<ProductFaqItemViewModel> FaqItems { get; init; } = System.Array.Empty<ProductFaqItemViewModel>();

    public IReadOnlyList<ProductCommentViewModel> Comments { get; init; } = System.Array.Empty<ProductCommentViewModel>();

    public IReadOnlyList<ProductSummaryViewModel> RelatedProducts { get; init; } = System.Array.Empty<ProductSummaryViewModel>();

    public ProductCommentFormModel NewComment { get; init; } = new();
}

public class ProductModuleViewModel
{
    public required string Title { get; init; }

    public string? Description { get; init; }

    public string? Duration { get; init; }
}

public class ProductStatisticViewModel
{
    public required string Label { get; init; }

    public required string Value { get; init; }

    public string? Tooltip { get; init; }
}

public class ProductFaqItemViewModel
{
    public required string Question { get; init; }

    public required string Answer { get; init; }
}

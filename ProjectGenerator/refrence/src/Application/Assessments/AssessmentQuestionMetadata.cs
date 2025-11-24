using System.Text.Json.Serialization;

namespace Arsis.Application.Assessments;

public sealed record AssessmentQuestionMetadata
{
    [JsonPropertyName("assessment")]
    public string? Assessment { get; init; }

    [JsonPropertyName("dimension")]
    public string? Dimension { get; init; }

    [JsonPropertyName("item")]
    public string? Item { get; init; }

    [JsonPropertyName("min")]
    public int? Min { get; init; }

    [JsonPropertyName("max")]
    public int? Max { get; init; }

    [JsonPropertyName("minLabel")]
    public string? MinLabel { get; init; }

    [JsonPropertyName("maxLabel")]
    public string? MaxLabel { get; init; }

    [JsonPropertyName("inventoryId")]
    public string? InventoryId { get; init; }

    [JsonPropertyName("dimensionA")]
    public string? DimensionA { get; init; }

    [JsonPropertyName("dimensionB")]
    public string? DimensionB { get; init; }
}

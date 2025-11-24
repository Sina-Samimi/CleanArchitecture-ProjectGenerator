using System.Text.Json.Serialization;

namespace Arsis.Application.Assessments;

public sealed record AssessmentRequest
{
    [JsonPropertyName("userId")]
    public long UserId { get; init; }

    [JsonPropertyName("inventoryId")]
    public string InventoryId { get; init; } = string.Empty;

    [JsonPropertyName("cilifton")]
    public IDictionary<string, IDictionary<string, int>> Cilifton { get; init; } =
        new Dictionary<string, IDictionary<string, int>>(StringComparer.OrdinalIgnoreCase);

    [JsonPropertyName("pvq")]
    public IDictionary<string, IDictionary<string, int>> Pvq { get; init; } =
        new Dictionary<string, IDictionary<string, int>>(StringComparer.OrdinalIgnoreCase);
}

public sealed record NormalizedScores(
    [property: JsonPropertyName("clifton")] IReadOnlyDictionary<string, double> Clifton,
    [property: JsonPropertyName("pvq")] IReadOnlyDictionary<string, double> Pvq);

public sealed record SkillPlan(
    [property: JsonPropertyName("jobGroup")] string JobGroup,
    [property: JsonPropertyName("top5_SelfAwareness")] IReadOnlyList<string> Top5SelfAwareness,
    [property: JsonPropertyName("top5_SelfBuilding")] IReadOnlyList<string> Top5SelfBuilding,
    [property: JsonPropertyName("top5_SelfDevelopment")] IReadOnlyList<string> Top5SelfDevelopment,
    [property: JsonPropertyName("top5_SelfActualization")] IReadOnlyList<string> Top5SelfActualization);

public sealed record AssessmentResponse(
    [property: JsonPropertyName("scores")] NormalizedScores Scores,
    [property: JsonPropertyName("jobGroups")] IReadOnlyDictionary<string, double> JobGroups,
    [property: JsonPropertyName("plans")] IReadOnlyList<SkillPlan> Plans);

public sealed record AssessmentDebugResponse(
    [property: JsonPropertyName("scores")] NormalizedScores Scores,
    [property: JsonPropertyName("gFromClifton")] IReadOnlyDictionary<string, double> JobGroupsFromClifton,
    [property: JsonPropertyName("gFromPvq")] IReadOnlyDictionary<string, double> JobGroupsFromPvq,
    [property: JsonPropertyName("gFinal")] IReadOnlyDictionary<string, double> JobGroupsFinal,
    [property: JsonPropertyName("sFromTalent")] IReadOnlyDictionary<string, double> SkillsFromTalent,
    [property: JsonPropertyName("sJob")] IReadOnlyDictionary<string, double> SelectedJobSkills,
    [property: JsonPropertyName("plans")] IReadOnlyList<SkillPlan> Plans);

public sealed record MatrixMetadata(
    string Name,
    int Rows,
    int Columns,
    DateTimeOffset LoadedAt,
    string SourcePath);

public sealed record MatricesOverview(
    IReadOnlyCollection<MatrixMetadata> Matrices,
    DateTimeOffset LoadedAt);

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Arsis.Domain.Entities.Assessments;

public class AssessmentQuestion
{
    [Key]
    public int Id { get; set; }

    public AssessmentTestType TestType { get; set; }

    public int Index { get; set; }

    // Clifton fields
    public string? TextA { get; set; }

    public string? TextB { get; set; }

    public string? TalentCodeA { get; set; }

    public string? TalentCodeB { get; set; }

    // PVQ fields
    public string? Text { get; set; }

    public string? PvqCode { get; set; }

    public bool? IsReverse { get; set; }

    public int? LikertMin { get; set; }

    public int? LikertMax { get; set; }

    [InverseProperty(nameof(AssessmentUserResponse.Question))]
    public ICollection<AssessmentUserResponse> Responses { get; set; } = new List<AssessmentUserResponse>();
}

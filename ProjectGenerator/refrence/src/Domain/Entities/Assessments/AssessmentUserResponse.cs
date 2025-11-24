using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Arsis.Domain.Entities.Assessments;

public class AssessmentUserResponse
{
    [Key]
    public int Id { get; set; }

    public int AssessmentRunId { get; set; }

    public AssessmentRun Run { get; set; } = default!;

    public int AssessmentQuestionId { get; set; }

    public AssessmentQuestion Question { get; set; } = default!;

    public string Answer { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}

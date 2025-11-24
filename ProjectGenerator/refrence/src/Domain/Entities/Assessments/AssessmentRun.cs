using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Arsis.Domain.Entities.Assessments;

public class AssessmentRun
{
    [Key]
    public int Id { get; set; }

    public int UserId { get; set; }

    public DateTime StartedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    [InverseProperty(nameof(AssessmentUserResponse.Run))]
    public ICollection<AssessmentUserResponse> Responses { get; set; } = new List<AssessmentUserResponse>();
}

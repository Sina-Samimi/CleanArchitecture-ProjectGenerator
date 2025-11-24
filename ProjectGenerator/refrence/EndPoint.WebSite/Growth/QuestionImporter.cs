using System.Linq;
using Arsis.Domain.Entities.Assessments;
using Arsis.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EndPoint.WebSite.Growth;

public sealed class QuestionImporter : IQuestionImporter
{
    private readonly AppDbContext _dbContext;

    public QuestionImporter(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task ImportCliftonAsync(string xlsxPath, CancellationToken cancellationToken)
    {
        _ = xlsxPath;
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        await _dbContext.AssessmentResponses.Where(r => r.Question.TestType == AssessmentTestType.Clifton).ExecuteDeleteAsync(cancellationToken);
        await _dbContext.AssessmentQuestions.Where(q => q.TestType == AssessmentTestType.Clifton).ExecuteDeleteAsync(cancellationToken);

        var questions = AssessmentQuestionsData.Clifton
            .OrderBy(item => item.Index)
            .Select(item => new AssessmentQuestion
            {
                TestType = AssessmentTestType.Clifton,
                Index = item.Index,
                TextA = item.TextA?.Trim(),
                TextB = item.TextB?.Trim(),
                TalentCodeA = NormalizeTalentCode(item.TalentCodeA),
                TalentCodeB = NormalizeTalentCode(item.TalentCodeB)
            })
            .ToList();

        await _dbContext.AssessmentQuestions.AddRangeAsync(questions, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task ImportPvqAsync(string csvPath, CancellationToken cancellationToken)
    {
        _ = csvPath;
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        await _dbContext.AssessmentResponses.Where(r => r.Question.TestType == AssessmentTestType.Pvq).ExecuteDeleteAsync(cancellationToken);
        await _dbContext.AssessmentQuestions.Where(q => q.TestType == AssessmentTestType.Pvq).ExecuteDeleteAsync(cancellationToken);

        var records = AssessmentQuestionsData.PVQ
            .OrderBy(item => item.Index)
            .Select(item => new AssessmentQuestion
            {
                TestType = AssessmentTestType.Pvq,
                Index = item.Index,
                Text = item.Text?.Trim(),
                PvqCode = NormalizePvqCode(item.PvqCode),
                IsReverse = item.IsReverse,
                LikertMin = item.LikertMin,
                LikertMax = item.LikertMax
            })
            .ToList();

        await _dbContext.AssessmentQuestions.AddRangeAsync(records, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private static string? NormalizeTalentCode(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var trimmed = raw.Trim().ToUpperInvariant();
        if (!trimmed.StartsWith("E", StringComparison.OrdinalIgnoreCase))
        {
            trimmed = $"E{trimmed}";
        }

        return trimmed;
    }

    private static string? NormalizePvqCode(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var trimmed = raw.Trim().ToUpperInvariant();
        if (!trimmed.StartsWith("A", StringComparison.OrdinalIgnoreCase))
        {
            trimmed = $"A{trimmed}";
        }

        return trimmed;
    }
}

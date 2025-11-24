using System;
using System.Diagnostics.CodeAnalysis;
using Arsis.Domain.Base;
using Arsis.Domain.Enums;

namespace Arsis.Domain.Entities;

public sealed class UserResponse : Entity
{
    public Guid UserId { get; private set; }

    public Guid QuestionId { get; private set; }

    public LikertScale Answer { get; private set; }

    public DateTimeOffset SubmittedAt { get; private set; }

    [SetsRequiredMembers]
    private UserResponse()
    {
        Answer = LikertScale.Neutral;
    }

    [SetsRequiredMembers]
    public UserResponse(Guid userId, Guid questionId, LikertScale answer, DateTimeOffset submittedAt)
    {
        UserId = userId;
        QuestionId = questionId;
        Answer = answer;
        SubmittedAt = submittedAt;
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void UpdateAnswer(LikertScale newAnswer, DateTimeOffset at)
    {
        Answer = newAnswer;
        SubmittedAt = at;
        UpdateDate = DateTimeOffset.UtcNow;
    }
}

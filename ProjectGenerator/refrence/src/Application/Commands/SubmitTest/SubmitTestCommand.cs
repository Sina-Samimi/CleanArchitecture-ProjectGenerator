using Arsis.Application.Abstractions.Messaging;
using Arsis.Application.DTOs;
using Arsis.Application.Interfaces;
using Arsis.Domain.Entities;
using Arsis.SharedKernel.BaseTypes;
using Arsis.SharedKernel.Constants;

namespace Arsis.Application.Commands.SubmitTest;

public sealed record SubmitTestCommand(SubmitTestDto Payload) : ICommand<SubmitTestResultDto>
{
    public sealed class Handler : ICommandHandler<SubmitTestCommand, SubmitTestResultDto>
    {
        private readonly SubmitTestValidator _validator = new();
        private readonly IQuestionRepository _questionRepository;
        private readonly ITalentRepository _talentRepository;
        private readonly ITestSubmissionRepository _testSubmissionRepository;
        private readonly ITalentScoreRepository _talentScoreRepository;
        private readonly ITalentScoreCalculator _talentScoreCalculator;
        private readonly IReportGenerator _reportGenerator;
        private readonly IAuditContext _auditContext;

        public Handler(
            IQuestionRepository questionRepository,
            ITalentRepository talentRepository,
            ITestSubmissionRepository testSubmissionRepository,
            ITalentScoreRepository talentScoreRepository,
            ITalentScoreCalculator talentScoreCalculator,
            IReportGenerator reportGenerator,
            IAuditContext auditContext)
        {
            _questionRepository = questionRepository;
            _talentRepository = talentRepository;
            _testSubmissionRepository = testSubmissionRepository;
            _talentScoreRepository = talentScoreRepository;
            _talentScoreCalculator = talentScoreCalculator;
            _reportGenerator = reportGenerator;
            _auditContext = auditContext;
        }

        public async Task<Result<SubmitTestResultDto>> Handle(SubmitTestCommand request, CancellationToken cancellationToken)
        {
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var error = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
                return Result<SubmitTestResultDto>.Failure(error);
            }

            var questions = await _questionRepository.GetAllAsync(cancellationToken);
            var talents = await _talentRepository.GetAllAsync(cancellationToken);

            var audit = _auditContext.Capture();

            var responses = MapResponses(request.Payload, questions, audit);

            await _testSubmissionRepository.SaveResponsesAsync(responses, cancellationToken);

            var scores = await _talentScoreCalculator.CalculateAsync(
                request.Payload.UserId,
                questions,
                talents,
                responses,
                audit,
                cancellationToken);

            await _talentScoreRepository.SaveScoresAsync(scores, cancellationToken);

            var topTalents = scores
                .OrderByDescending(score => score.Score.Value)
                .Take(TestConstants.DefaultTopTalentCount)
                .Select(score =>
                {
                    var talent = talents.First(t => t.Id == score.TalentId);
                    return new TalentScoreDto(talent.Id, talent.Name, score.Score.Value);
                })
                .ToList();

            var report = await _reportGenerator.GenerateAsync(
                new ReportRequestDto(request.Payload.UserId, topTalents),
                cancellationToken);

            return Result<SubmitTestResultDto>.Success(new SubmitTestResultDto(
                request.Payload.UserId,
                topTalents,
                report));
        }

        private static IReadOnlyCollection<UserResponse> MapResponses(
            SubmitTestDto payload,
            IReadOnlyCollection<Question> questions,
            AuditMetadata audit)
        {
            var questionLookup = questions.ToDictionary(q => q.Id);
            var responses = new List<UserResponse>();

            foreach (var responseDto in payload.Responses)
            {
                if (!questionLookup.TryGetValue(responseDto.QuestionId, out var question))
                {
                    throw new KeyNotFoundException($"Question with id '{responseDto.QuestionId}' was not found.");
                }

                responses.Add(new UserResponse(payload.UserId, question.Id, responseDto.Answer, DateTimeOffset.UtcNow)
                {
                    CreatorId = audit.UserId,
                    CreateDate = audit.Timestamp,
                    UpdateDate = audit.Timestamp,
                    Ip = audit.IpAddress
                });
            }

            return responses;
        }
    }
}

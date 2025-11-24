using Arsis.Domain.Enums;

namespace Arsis.Application.DTOs;

public sealed record SubmitTestDto(Guid UserId, IReadOnlyCollection<UserResponseDto> Responses);

public sealed record UserResponseDto(Guid QuestionId, LikertScale Answer);

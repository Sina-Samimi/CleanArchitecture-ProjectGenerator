using Arsis.Application.DTOs;
using FluentValidation;

namespace Arsis.Application.Commands.SubmitTest;

public sealed class SubmitTestValidator : AbstractValidator<SubmitTestCommand>
{
    public SubmitTestValidator()
    {
        RuleFor(x => x.Payload.UserId).NotEmpty();
        RuleFor(x => x.Payload.Responses)
            .NotNull()
            .NotEmpty();
        RuleForEach(x => x.Payload.Responses).SetValidator(new UserResponseDtoValidator());
    }

    private sealed class UserResponseDtoValidator : AbstractValidator<UserResponseDto>
    {
        public UserResponseDtoValidator()
        {
            RuleFor(x => x.QuestionId).NotEmpty();
            RuleFor(x => x.Answer).IsInEnum();
        }
    }
}

using ChallengeGate.Services;
using ChallengeGate.ViewModels;
using FluentValidation;

namespace ChallengeGate.Validators;

public class ChallengeViewModelValidator : AbstractValidator<ChallengeViewModel>
{
    public ChallengeViewModelValidator(IPasswordMatcher passwordMatcher)
    {
        RuleFor(x => x.Password)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage(ChallengeGateConstants.ErrorMessages.PasswordRequired)
            .Must(passwordMatcher.Matches)
            .WithMessage(ChallengeGateConstants.ErrorMessages.PasswordIncorrect);
    }
}

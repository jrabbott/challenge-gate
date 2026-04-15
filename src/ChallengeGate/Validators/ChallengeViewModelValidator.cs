using ChallengeGate.Configuration;
using ChallengeGate.ViewModels;
using System.Security.Cryptography;
using System.Text;
using FluentValidation;
using Microsoft.Extensions.Options;

namespace ChallengeGate.Validators;

public class ChallengeViewModelValidator : AbstractValidator<ChallengeViewModel>
{
    private readonly ChallengeOptions _options;

    public ChallengeViewModelValidator(IOptions<ChallengeOptions> options)
    {
        _options = options.Value;

        RuleFor(x => x.Password)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage(ChallengeGateConstants.ErrorMessages.PasswordRequired)
            .Must(BeCorrectPassword)
            .WithMessage(ChallengeGateConstants.ErrorMessages.PasswordIncorrect);
    }

    private bool BeCorrectPassword(string? password)
    {
        if (password == null || string.IsNullOrEmpty(_options.Password))
        {
            return false;
        }

        // Use constant-time comparison to prevent timing attacks
        var passwordBytes = Encoding.UTF8.GetBytes(password);
        var expectedBytes = Encoding.UTF8.GetBytes(_options.Password);

        return passwordBytes.Length == expectedBytes.Length && CryptographicOperations.FixedTimeEquals(passwordBytes, expectedBytes);
    }
}

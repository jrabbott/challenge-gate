using ChallengeGate.Configuration;
using ChallengeGate.Services;
using ChallengeGate.Validators;
using ChallengeGate.ViewModels;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;

namespace ChallengeGate.Tests.Validators;

public class ChallengeViewModelValidatorTests
{
    private const string CorrectPassword = "password123";

    private static ChallengeViewModelValidator CreateValidator(string configuredPassword)
    {
        var options = new ChallengeOptions { Password = configuredPassword };
        var dataProtectionProvider = new EphemeralDataProtectionProvider();
        var challengeGateService = new ChallengeGateService(Options.Create(options), dataProtectionProvider);
        return new ChallengeViewModelValidator(challengeGateService);
    }

    [Fact]
    public void Validate_WhenPasswordIsCorrect_ReturnsValid()
    {
        var validator = CreateValidator(CorrectPassword);
        var model = new ChallengeViewModel { Password = CorrectPassword };

        var result = validator.Validate(model);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WhenPasswordIsEmpty_ReturnsInvalid()
    {
        var validator = CreateValidator(CorrectPassword);
        var model = new ChallengeViewModel { Password = "" };

        var result = validator.Validate(model);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Enter the password");
    }

    [Fact]
    public void Validate_WhenPasswordIsNull_ReturnsInvalid()
    {
        var validator = CreateValidator(CorrectPassword);
        var model = new ChallengeViewModel { Password = null };

        var result = validator.Validate(model);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Enter the password");
    }

    [Fact]
    public void Validate_WhenPasswordIsIncorrect_ReturnsInvalid()
    {
        var validator = CreateValidator(CorrectPassword);
        var model = new ChallengeViewModel { Password = "wrong-password" };

        var result = validator.Validate(model);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "The password you entered is incorrect");
    }

    [Fact]
    public void Validate_WhenPasswordIsIncorrectButSameLength_ReturnsInvalid()
    {
        var validator = CreateValidator(CorrectPassword);
        var model = new ChallengeViewModel { Password = "password124" }; // length 11

        var result = validator.Validate(model);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "The password you entered is incorrect");
    }

    [Fact]
    public void Validate_WhenConfiguredPasswordIsEmpty_ReturnsInvalid()
    {
        var validator = CreateValidator("");
        var model = new ChallengeViewModel { Password = "some-password" };

        var result = validator.Validate(model);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "The password you entered is incorrect");
    }
}

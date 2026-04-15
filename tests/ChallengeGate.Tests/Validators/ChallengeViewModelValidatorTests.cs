using ChallengeGate.Configuration;
using ChallengeGate.Validators;
using ChallengeGate.ViewModels;
using Microsoft.Extensions.Options;

namespace ChallengeGate.Tests.Validators;

public class ChallengeViewModelValidatorTests
{
    private const string CorrectPassword = "password123";

    [Fact]
    public void Validate_WhenPasswordIsCorrect_ReturnsValid()
    {
        // Arrange
        var options = new ChallengeOptions { Password = CorrectPassword };
        var validator = new ChallengeViewModelValidator(Options.Create(options));
        var model = new ChallengeViewModel { Password = CorrectPassword };

        // Act
        var result = validator.Validate(model);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WhenPasswordIsEmpty_ReturnsInvalid()
    {
        // Arrange
        var options = new ChallengeOptions { Password = CorrectPassword };
        var validator = new ChallengeViewModelValidator(Options.Create(options));
        var model = new ChallengeViewModel { Password = "" };

        // Act
        var result = validator.Validate(model);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Enter the password");
    }

    [Fact]
    public void Validate_WhenPasswordIsNull_ReturnsInvalid()
    {
        // Arrange
        var options = new ChallengeOptions { Password = CorrectPassword };
        var validator = new ChallengeViewModelValidator(Options.Create(options));
        var model = new ChallengeViewModel { Password = null };

        // Act
        var result = validator.Validate(model);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Enter the password");
    }

    [Fact]
    public void Validate_WhenPasswordIsIncorrect_ReturnsInvalid()
    {
        // Arrange
        var options = new ChallengeOptions { Password = CorrectPassword };
        var validator = new ChallengeViewModelValidator(Options.Create(options));
        var model = new ChallengeViewModel { Password = "wrong-password" };

        // Act
        var result = validator.Validate(model);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "The password you entered is incorrect");
    }

    [Fact]
    public void Validate_WhenPasswordIsIncorrectButSameLength_ReturnsInvalid()
    {
        // Arrange
        var options = new ChallengeOptions { Password = CorrectPassword }; // length 11
        var validator = new ChallengeViewModelValidator(Options.Create(options));
        var model = new ChallengeViewModel { Password = "password124" }; // length 11

        // Act
        var result = validator.Validate(model);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "The password you entered is incorrect");
    }

    [Fact]
    public void Validate_WhenConfiguredPasswordIsEmpty_ReturnsInvalid()
    {
        // Arrange
        var options = new ChallengeOptions { Password = "" };
        var validator = new ChallengeViewModelValidator(Options.Create(options));
        var model = new ChallengeViewModel { Password = "some-password" };

        // Act
        var result = validator.Validate(model);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "The password you entered is incorrect");
    }
}

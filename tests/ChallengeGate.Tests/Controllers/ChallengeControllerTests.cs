using ChallengeGate.Configuration;
using ChallengeGate.Controllers;
using ChallengeGate.Validators;
using ChallengeGate.ViewModels;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Moq;

namespace ChallengeGate.Tests.Controllers;

public class ChallengeControllerTests
{
    private readonly ChallengeOptions _options;
    private readonly IDataProtectionProvider _dataProtectionProvider;
    private readonly ChallengeController _controller;

    public ChallengeControllerTests()
    {
        _options = new ChallengeOptions
        {
            Enabled = true,
            Password = "password123",
            Layout = "_CustomLayout",
            Title = "Custom Title"
        };
        _dataProtectionProvider = new EphemeralDataProtectionProvider();
        _controller = new ChallengeController(
            Options.Create(_options), 
            new ChallengeViewModelValidator(Options.Create(_options)),
            _dataProtectionProvider);
    }

    [Fact]
    public void Index_Get_ReturnsViewModel()
    {
        // Act
        var result = _controller.Index("/return") as ViewResult;

        // Assert
        Assert.NotNull(result);
        var model = Assert.IsType<ChallengeViewModel>(result.Model);
        Assert.Equal("_CustomLayout", model.Layout);
        Assert.Equal("Custom Title", model.Title);
        Assert.Equal("/return", model.ReturnUrl);
    }

    [Fact]
    public void Index_Post_CorrectPassword_SetsSignedCookieAndRedirects()
    {
        // Arrange
        var mockResponse = new Mock<HttpResponse>();
        var mockCookies = new Mock<IResponseCookies>();
        mockResponse.SetupGet(r => r.Cookies).Returns(mockCookies.Object);

        var mockContext = new Mock<HttpContext>();
        mockContext.SetupGet(c => c.Response).Returns(mockResponse.Object);
        
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = mockContext.Object
        };

        var model = new ChallengeViewModel { Password = "password123", ReturnUrl = "/return" };

        // Act
        var result = _controller.Index(model) as RedirectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal("/return", result.Url);
        
        // Verify that Append was called with a protected value
        mockCookies.Verify(c => c.Append(
            _options.CookieName, 
            It.Is<string>(v => v.Length > 20), // Protected strings are long
            It.IsAny<CookieOptions>()), 
            Times.Once);
    }

    [Fact]
    public void Index_Post_IncorrectPassword_ReturnsViewWithError()
    {
        // Arrange
        var model = new ChallengeViewModel { Password = "wrong", ReturnUrl = "/return" };

        // Act
        var result = _controller.Index(model) as ViewResult;

        // Assert
        Assert.NotNull(result);
        Assert.False(_controller.ModelState.IsValid);
        var returnModel = Assert.IsType<ChallengeViewModel>(result.Model);
        Assert.Equal("_CustomLayout", returnModel.Layout);
        Assert.Equal("Custom Title", returnModel.Title);
    }

    [Fact]
    public void Index_Post_EmptyPassword_FailsValidation()
    {
        // Arrange
        var model = new ChallengeViewModel { Password = "", ReturnUrl = "/return" };

        // Act
        var result = _controller.Index(model) as ViewResult;

        // Assert
        Assert.NotNull(result);
        Assert.False(_controller.ModelState.IsValid);
        Assert.True(_controller.ModelState.ContainsKey(nameof(ChallengeViewModel.Password)));
        Assert.Equal("Enter the password", _controller.ModelState[nameof(ChallengeViewModel.Password)]?.Errors[0].ErrorMessage);
    }

    [Fact]
    public void Index_Get_WhenDisabled_Redirects()
    {
        // Arrange
        var options = new ChallengeOptions { Enabled = false };
        var controller = new ChallengeController(
            Options.Create(options), 
            new ChallengeViewModelValidator(Options.Create(options)),
            _dataProtectionProvider);

        // Act
        var result = controller.Index("/custom-return") as RedirectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal("/custom-return", result.Url);
    }

    [Fact]
    public void Index_Post_WhenDisabled_Redirects()
    {
        // Arrange
        var options = new ChallengeOptions { Enabled = false };
        var controller = new ChallengeController(
            Options.Create(options), 
            new ChallengeViewModelValidator(Options.Create(options)),
            _dataProtectionProvider);
        var model = new ChallengeViewModel { ReturnUrl = "/custom-return" };

        // Act
        var result = controller.Index(model) as RedirectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal("/custom-return", result.Url);
    }
}

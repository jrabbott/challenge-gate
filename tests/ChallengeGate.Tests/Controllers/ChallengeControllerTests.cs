using ChallengeGate.Configuration;
using ChallengeGate.Controllers;
using ChallengeGate.Services;
using ChallengeGate.Validators;
using ChallengeGate.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Moq;

namespace ChallengeGate.Tests.Controllers;

public class ChallengeControllerTests
{
    private readonly ChallengeController _controller;
    private readonly Mock<IPasswordMatcher> _passwordMatcherMock;
    private readonly Mock<IChallengeGateAuthenticator> _authenticatorMock;

    public ChallengeControllerTests()
    {
        ChallengeOptions options = new()
        {
            Enabled = true,
            Password = "password123",
            Layout = "_CustomLayout",
            Title = "Custom Title"
        };
        
        _passwordMatcherMock = new Mock<IPasswordMatcher>();
        _authenticatorMock = new Mock<IChallengeGateAuthenticator>();
        
        _controller = new ChallengeController(
            Options.Create(options), 
            new ChallengeViewModelValidator(_passwordMatcherMock.Object),
            _authenticatorMock.Object);
    }

    [Fact]
    public void Index_Get_ReturnsViewModel()
    {
        var result = _controller.Index("/return") as ViewResult;

        Assert.NotNull(result);
        var model = Assert.IsType<ChallengeViewModel>(result.Model);
        Assert.Equal("_CustomLayout", model.Layout);
        Assert.Equal("Custom Title", model.Title);
        Assert.Equal("/return", model.ReturnUrl);
    }

    [Fact]
    public void Index_Post_CorrectPassword_IssuesCookieAndRedirects()
    {
        var mockContext = new Mock<HttpContext>();
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = mockContext.Object
        };

        var model = new ChallengeViewModel { Password = "password123", ReturnUrl = "/return" };
        _passwordMatcherMock.Setup(s => s.Matches("password123")).Returns(true);

        var result = _controller.Index(model) as RedirectResult;

        Assert.NotNull(result);
        Assert.Equal("/return", result.Url);
        _authenticatorMock.Verify(s => s.IssueCookie(mockContext.Object), Times.Once);
    }

    [Fact]
    public void Index_Post_IncorrectPassword_ReturnsViewWithError()
    {
        var model = new ChallengeViewModel { Password = "wrong", ReturnUrl = "/return" };
        _passwordMatcherMock.Setup(s => s.Matches("wrong")).Returns(false);

        var result = _controller.Index(model) as ViewResult;

        Assert.NotNull(result);
        Assert.False(_controller.ModelState.IsValid);
        var returnModel = Assert.IsType<ChallengeViewModel>(result.Model);
        Assert.Equal("_CustomLayout", returnModel.Layout);
        Assert.Equal("Custom Title", returnModel.Title);
    }

    [Fact]
    public void Index_Post_EmptyPassword_FailsValidation()
    {
        var model = new ChallengeViewModel { Password = "", ReturnUrl = "/return" };

        var result = _controller.Index(model) as ViewResult;

        Assert.NotNull(result);
        Assert.False(_controller.ModelState.IsValid);
        Assert.True(_controller.ModelState.ContainsKey(nameof(ChallengeViewModel.Password)));
    }

    [Fact]
    public void Index_Get_WhenDisabled_Redirects()
    {
        var options = new ChallengeOptions { Enabled = false };
        var controller = new ChallengeController(
            Options.Create(options), 
            new ChallengeViewModelValidator(_passwordMatcherMock.Object),
            _authenticatorMock.Object);

        var result = controller.Index("/custom-return") as RedirectResult;

        Assert.NotNull(result);
        Assert.Equal("/custom-return", result.Url);
    }

    [Fact]
    public void Index_Post_WhenDisabled_Redirects()
    {
        var options = new ChallengeOptions { Enabled = false };
        var controller = new ChallengeController(
            Options.Create(options), 
            new ChallengeViewModelValidator(_passwordMatcherMock.Object),
            _authenticatorMock.Object);
        var model = new ChallengeViewModel { ReturnUrl = "/custom-return" };

        var result = controller.Index(model) as RedirectResult;

        Assert.NotNull(result);
        Assert.Equal("/custom-return", result.Url);
    }
}

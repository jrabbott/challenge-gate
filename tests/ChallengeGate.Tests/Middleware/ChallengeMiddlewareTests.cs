using ChallengeGate.Configuration;
using ChallengeGate.Middleware;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Moq;

namespace ChallengeGate.Tests.Middleware;

public class ChallengeMiddlewareTests
{
    private readonly Mock<RequestDelegate> _nextMock;
    private readonly IDataProtectionProvider _dataProtectionProvider;
    private readonly IDataProtector _protector;

    public ChallengeMiddlewareTests()
    {
        _nextMock = new Mock<RequestDelegate>();
        _dataProtectionProvider = new EphemeralDataProtectionProvider();
        _protector = _dataProtectionProvider.CreateProtector("ChallengeGate.Auth");
    }

    [Fact]
    public async Task InvokeAsync_WhenDisabled_CallsNext()
    {
        // Arrange
        var options = new ChallengeOptions { Enabled = false };
        var middleware = new ChallengeMiddleware(_nextMock.Object, Options.Create(options), _dataProtectionProvider);
        var context = new DefaultHttpContext();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _nextMock.Verify(n => n(context), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WhenRequestIsChallengePath_CallsNext()
    {
        // Arrange
        var options = new ChallengeOptions { Enabled = true, ChallengePath = "/challenge" };
        var middleware = new ChallengeMiddleware(_nextMock.Object, Options.Create(options), _dataProtectionProvider);
        var context = new DefaultHttpContext();
        context.Request.Path = "/challenge";

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _nextMock.Verify(n => n(context), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WhenPathIsBypassed_CallsNext()
    {
        // Arrange
        var options = new ChallengeOptions 
        { 
            Enabled = true, 
            BypassPaths = new List<string> { "/css" } 
        };
        var middleware = new ChallengeMiddleware(_nextMock.Object, Options.Create(options), _dataProtectionProvider);
        var context = new DefaultHttpContext();
        context.Request.Path = "/css/site.css";

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _nextMock.Verify(n => n(context), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WhenUnauthorized_RedirectsToChallenge()
    {
        // Arrange
        var options = new ChallengeOptions 
        { 
            Enabled = true, 
            ChallengePath = "/challenge",
            CookieName = "test-cookie"
        };
        var middleware = new ChallengeMiddleware(_nextMock.Object, Options.Create(options), _dataProtectionProvider);
        var context = new DefaultHttpContext();
        context.Request.Path = "/secure";

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(302, context.Response.StatusCode);
        var location = context.Response.Headers["Location"].ToString();
        Assert.StartsWith("/challenge", location);
        Assert.Contains("returnUrl=%2Fsecure", location);
        _nextMock.Verify(n => n(context), Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_WhenAuthorizedBySignedCookie_CallsNext()
    {
        // Arrange
        var options = new ChallengeOptions 
        { 
            Enabled = true, 
            CookieName = "test-cookie",
            Password = "correct-password"
        };
        var middleware = new ChallengeMiddleware(_nextMock.Object, Options.Create(options), _dataProtectionProvider);
        var context = new DefaultHttpContext();
        context.Request.Path = "/secure";
        
        var protectedValue = _protector.Protect("Authorized:correct-password");
        context.Request.Headers.Append("Cookie", $"test-cookie={protectedValue}");

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _nextMock.Verify(n => n(context), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WhenPasswordChanged_RedirectsToChallenge()
    {
        // Arrange
        var options = new ChallengeOptions 
        { 
            Enabled = true, 
            CookieName = "test-cookie",
            Password = "new-password" // Password has changed
        };
        var middleware = new ChallengeMiddleware(_nextMock.Object, Options.Create(options), _dataProtectionProvider);
        var context = new DefaultHttpContext();
        context.Request.Path = "/secure";
        
        // Cookie was created with the OLD password
        var oldProtectedValue = _protector.Protect("Authorized:old-password");
        context.Request.Headers.Append("Cookie", $"test-cookie={oldProtectedValue}");

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(302, context.Response.StatusCode);
        _nextMock.Verify(n => n(context), Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_WhenCookieIsMalformed_RedirectsToChallenge()
    {
        // Arrange
        var options = new ChallengeOptions 
        { 
            Enabled = true, 
            ChallengePath = "/challenge",
            CookieName = "test-cookie"
        };
        var middleware = new ChallengeMiddleware(_nextMock.Object, Options.Create(options), _dataProtectionProvider);
        var context = new DefaultHttpContext();
        context.Request.Path = "/secure";
        context.Request.Headers.Append("Cookie", "test-cookie=some-malformed-nonsense-that-cannot-be-unprotected");

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(302, context.Response.StatusCode);
        _nextMock.Verify(n => n(context), Times.Never);
    }
}

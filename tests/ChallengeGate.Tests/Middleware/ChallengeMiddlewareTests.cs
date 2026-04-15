using ChallengeGate.Configuration;
using ChallengeGate.Middleware;
using ChallengeGate.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Moq;

namespace ChallengeGate.Tests.Middleware;

public class ChallengeMiddlewareTests
{
    private readonly Mock<RequestDelegate> _nextMock = new();
    private readonly Mock<IPasswordMatcher> _passwordMatcherMock = new();
    private readonly Mock<IChallengeGateAuthenticator> _authenticatorMock = new();

    private ChallengeMiddleware CreateMiddleware(ChallengeOptions options)
    {
        return new ChallengeMiddleware(
            _nextMock.Object, 
            Options.Create(options), 
            _passwordMatcherMock.Object,
            _authenticatorMock.Object);
    }

    [Fact]
    public async Task InvokeAsync_WhenDisabled_CallsNext()
    {
        ChallengeOptions options = new() { Enabled = false };
        var middleware = CreateMiddleware(options);
        var context = new DefaultHttpContext();

        await middleware.InvokeAsync(context);

        _nextMock.Verify(n => n(context), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WhenRequestIsChallengePath_CallsNext()
    {
        var options = new ChallengeOptions { Enabled = true, ChallengePath = "/challenge" };
        var middleware = CreateMiddleware(options);
        var context = new DefaultHttpContext
        {
            Request =
            {
                Path = "/challenge"
            }
        };

        await middleware.InvokeAsync(context);

        _nextMock.Verify(n => n(context), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WhenPathIsBypassed_CallsNext()
    {
        var options = new ChallengeOptions 
        { 
            Enabled = true, 
            BypassPaths = ["/css"] 
        };
        var middleware = CreateMiddleware(options);
        var context = new DefaultHttpContext
        {
            Request =
            {
                Path = "/css/site.css"
            }
        };

        await middleware.InvokeAsync(context);

        _nextMock.Verify(n => n(context), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WhenValidTokenInUrl_IssuesCookieAndRedirects()
    {
        var options = new ChallengeOptions 
        { 
            Enabled = true, 
            TokenQueryParamName = "token"
        };
        _passwordMatcherMock.Setup(v => v.Matches("correct-password")).Returns(true);
        
        var middleware = CreateMiddleware(options);
        var context = new DefaultHttpContext
        {
            Request =
            {
                Path = "/secure",
                QueryString = new QueryString("?token=correct-password")
            }
        };

        await middleware.InvokeAsync(context);

        Assert.Equal(302, context.Response.StatusCode);
        Assert.Equal("/secure", context.Response.Headers["Location"]);
        _authenticatorMock.Verify(s => s.IssueCookie(context), Times.Once);
        _nextMock.Verify(n => n(context), Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_WhenValidTokenInUrlWithOtherParams_RedirectsKeepingOtherParams()
    {
        var options = new ChallengeOptions 
        { 
            Enabled = true, 
            TokenQueryParamName = "token"
        };
        _passwordMatcherMock.Setup(v => v.Matches("correct-password")).Returns(true);
        
        var middleware = CreateMiddleware(options);
        var context = new DefaultHttpContext
        {
            Request =
            {
                Path = "/secure",
                QueryString = new QueryString("?foo=bar&token=correct-password&baz=qux")
            }
        };

        await middleware.InvokeAsync(context);

        Assert.Equal(302, context.Response.StatusCode);
        var location = context.Response.Headers.Location.ToString();
        Assert.Contains("foo=bar", location);
        Assert.Contains("baz=qux", location);
        Assert.DoesNotContain("token=correct-password", location);
        _nextMock.Verify(n => n(context), Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_WhenInvalidTokenInUrl_RedirectsToChallenge()
    {
        var options = new ChallengeOptions 
        { 
            Enabled = true, 
            TokenQueryParamName = "token",
            ChallengePath = "/challenge"
        };
        _passwordMatcherMock.Setup(v => v.Matches("wrong-password")).Returns(false);
        _authenticatorMock.Setup(s => s.IsAuthenticated(It.IsAny<HttpContext>())).Returns(false);
        
        var middleware = CreateMiddleware(options);
        var context = new DefaultHttpContext
        {
            Request =
            {
                Path = "/secure",
                QueryString = new QueryString("?token=wrong-password")
            }
        };

        await middleware.InvokeAsync(context);

        Assert.Equal(302, context.Response.StatusCode);
        Assert.StartsWith("/challenge", context.Response.Headers["Location"].ToString());
    }

    [Fact]
    public async Task InvokeAsync_WhenAuthorizedByService_CallsNext()
    {
        var options = new ChallengeOptions { Enabled = true };
        _authenticatorMock.Setup(s => s.IsAuthenticated(It.IsAny<HttpContext>())).Returns(true);
        
        var middleware = CreateMiddleware(options);
        var context = new DefaultHttpContext
        {
            Request =
            {
                Path = "/secure"
            }
        };

        await middleware.InvokeAsync(context);

        _nextMock.Verify(n => n(context), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WhenUnauthorized_RedirectsToChallenge()
    {
        var options = new ChallengeOptions 
        { 
            Enabled = true, 
            ChallengePath = "/challenge"
        };
        _authenticatorMock.Setup(s => s.IsAuthenticated(It.IsAny<HttpContext>())).Returns(false);
        
        var middleware = CreateMiddleware(options);
        var context = new DefaultHttpContext
        {
            Request =
            {
                Path = "/secure"
            }
        };

        await middleware.InvokeAsync(context);

        Assert.Equal(302, context.Response.StatusCode);
        var location = context.Response.Headers.Location.ToString();
        Assert.StartsWith("/challenge", location);
        Assert.Contains("returnUrl=%2Fsecure", location);
        _nextMock.Verify(n => n(context), Times.Never);
    }
}

using ChallengeGate.Configuration;
using ChallengeGate.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace ChallengeGate.Tests.Services;

public class ChallengeGateServiceTests
{
    private const string CorrectPassword = "password123";
    private readonly EphemeralDataProtectionProvider _dataProtectionProvider = new();

    [Fact]
    public void Matches_WhenPasswordIsCorrect_ReturnsTrue()
    {
        ChallengeOptions options = new() { Password = CorrectPassword };
        var service = new ChallengeGateService(Options.Create(options), _dataProtectionProvider);

        var result = service.Matches(CorrectPassword);

        Assert.True(result);
    }

    [Fact]
    public void Matches_WhenPasswordIsIncorrect_ReturnsFalse()
    {
        ChallengeOptions options = new() { Password = CorrectPassword };
        var service = new ChallengeGateService(Options.Create(options), _dataProtectionProvider);

        var result = service.Matches("wrong-password");

        Assert.False(result);
    }

    [Fact]
    public void Matches_WhenPasswordIsEmpty_ReturnsFalse()
    {
        ChallengeOptions options = new() { Password = CorrectPassword };
        var service = new ChallengeGateService(Options.Create(options), _dataProtectionProvider);

        var result = service.Matches("");

        Assert.False(result);
    }

    [Fact]
    public void IsAuthenticated_WhenNoCookiePresent_ReturnsFalse()
    {
        ChallengeOptions options = new() { Password = CorrectPassword, CookieName = "test-cookie" };
        var service = new ChallengeGateService(Options.Create(options), _dataProtectionProvider);
        var context = new DefaultHttpContext();

        var result = service.IsAuthenticated(context);

        Assert.False(result);
    }

    [Fact]
    public void IssueCookie_AppendsCookieToResponse()
    {
        ChallengeOptions options = new() { Password = CorrectPassword, CookieName = "test-cookie" };
        var service = new ChallengeGateService(Options.Create(options), _dataProtectionProvider);
        var context = new DefaultHttpContext();
        
        service.IssueCookie(context);

        Assert.True(context.Response.Headers.ContainsKey("Set-Cookie"));
        Assert.Contains("test-cookie=", context.Response.Headers["Set-Cookie"].ToString());
    }

    [Fact]
    public void IsAuthenticated_WhenCookieIsValid_ReturnsTrue()
    {
        ChallengeOptions options = new() { Password = CorrectPassword, CookieName = "test-cookie" };
        var service = new ChallengeGateService(Options.Create(options), _dataProtectionProvider);
        var context = new DefaultHttpContext();
        var protector = _dataProtectionProvider.CreateProtector("ChallengeGate.Auth");
        var protectedValue = protector.Protect($"Authorized:{CorrectPassword}");
        context.Request.Headers.Append("Cookie", $"test-cookie={protectedValue}");

        var result = service.IsAuthenticated(context);

        Assert.True(result);
    }

    [Fact]
    public void IsAuthenticated_WhenCookieIsInvalid_ReturnsFalse()
    {
        ChallengeOptions options = new() { Password = CorrectPassword, CookieName = "test-cookie" };
        var service = new ChallengeGateService(Options.Create(options), _dataProtectionProvider);
        var context = new DefaultHttpContext();
        context.Request.Headers.Append("Cookie", "test-cookie=invalid-value");

        var result = service.IsAuthenticated(context);

        Assert.False(result);
    }

    [Fact]
    public void IsAuthenticated_WhenPasswordChanged_ReturnsFalse()
    {
        ChallengeOptions options = new() { Password = "new-password", CookieName = "test-cookie" };
        var service = new ChallengeGateService(Options.Create(options), _dataProtectionProvider);
        var context = new DefaultHttpContext();
        var protector = _dataProtectionProvider.CreateProtector("ChallengeGate.Auth");
        var protectedValue = protector.Protect($"Authorized:old-password");
        context.Request.Headers.Append("Cookie", $"test-cookie={protectedValue}");

        var result = service.IsAuthenticated(context);

        Assert.False(result);
    }
}

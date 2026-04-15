using System.Security.Cryptography;
using System.Text;
using ChallengeGate.Configuration;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace ChallengeGate.Services;

public interface IPasswordMatcher
{
    bool Matches(string? password);
}

public interface IChallengeGateAuthenticator
{
    void IssueCookie(HttpContext context);
    bool IsAuthenticated(HttpContext context);
}

public class ChallengeGateService(
    IOptions<ChallengeOptions> options,
    IDataProtectionProvider dataProtectionProvider) : IPasswordMatcher, IChallengeGateAuthenticator
{
    private readonly ChallengeOptions _options = options.Value;
    private readonly IDataProtector _protector = dataProtectionProvider.CreateProtector(ChallengeGateConstants.DataProtectionPurpose);

    public bool Matches(string? password)
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

    public void IssueCookie(HttpContext context)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddMinutes(_options.CookieExpirationMinutes)
        };

        var protectedValue = _protector.Protect($"{ChallengeGateConstants.CookieValuePrefix}:{_options.Password}");
        context.Response.Cookies.Append(_options.CookieName, protectedValue, cookieOptions);
    }

    public bool IsAuthenticated(HttpContext context)
    {
        if (context.Request.Cookies.TryGetValue(_options.CookieName, out var cookieValue))
        {
            try
            {
                var unprotectedValue = _protector.Unprotect(cookieValue);
                return unprotectedValue == $"{ChallengeGateConstants.CookieValuePrefix}:{_options.Password}";
            }
            catch
            {
                return false;
            }
        }

        return false;
    }
}

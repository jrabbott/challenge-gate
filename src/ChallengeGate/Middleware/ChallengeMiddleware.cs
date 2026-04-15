using ChallengeGate.Configuration;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace ChallengeGate.Middleware;

public class ChallengeMiddleware(
    RequestDelegate next,
    IOptions<ChallengeOptions> options,
    IDataProtectionProvider dataProtectionProvider)
{
    private readonly ChallengeOptions _options = options.Value;
    private readonly IDataProtector _protector = dataProtectionProvider.CreateProtector(ChallengeGateConstants.DataProtectionPurpose);     

    public async Task InvokeAsync(HttpContext context)
    {
        if (!_options.Enabled)
        {
            await next(context);
            return;
        }

        var path = context.Request.Path.Value ?? string.Empty;

        // Allow access to the challenge page itself
        if (path.Equals(_options.ChallengePath, StringComparison.OrdinalIgnoreCase))
        {
            await next(context);
            return;
        }

        // Check bypass paths
        if (_options.BypassPaths.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
        {
            await next(context);
            return;
        }

        if (context.Request.Cookies.TryGetValue(_options.CookieName, out var cookieValue))
        {
            try
            {
                // Unprotect the cookie and verify it matches the current password.
                // If the password was changed in configuration, the existing cookie will fail this check.
                var unprotectedValue = _protector.Unprotect(cookieValue);
                
                if (unprotectedValue == $"{ChallengeGateConstants.CookieValuePrefix}:{_options.Password}")
                {
                    await next(context);
                    return;
                }
            }
            catch
            {
                // Ignore malformed, expired, or invalid cookies and fall through to re-authentication.
            }
        }

        var returnUrl = context.Request.Path + context.Request.QueryString;
        context.Response.Redirect($"{_options.ChallengePath}?returnUrl={Uri.EscapeDataString(returnUrl)}");
    }
}

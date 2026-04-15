using ChallengeGate.Configuration;
using ChallengeGate.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Options;

namespace ChallengeGate.Middleware;

public class ChallengeMiddleware(
    RequestDelegate next,
    IOptions<ChallengeOptions> options,
    IPasswordMatcher passwordMatcher,
    IChallengeGateAuthenticator authenticator)
{
    private readonly ChallengeOptions _options = options.Value;

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

        // Check query string token bypass
        if (context.Request.Query.TryGetValue(_options.TokenQueryParamName, out var tokenValues))
        {
            var token = tokenValues.FirstOrDefault();
            if (passwordMatcher.Matches(token))
            {
                authenticator.IssueCookie(context);

                var query = context.Request.Query.ToDictionary(q => q.Key, q => q.Value);
                query.Remove(_options.TokenQueryParamName);
                
                var queryBuilder = new QueryBuilder(query);
                var redirectUrl = context.Request.PathBase + context.Request.Path + queryBuilder.ToQueryString();
                
                context.Response.Redirect(redirectUrl);
                return;
            }
        }

        if (authenticator.IsAuthenticated(context))
        {
            await next(context);
            return;
        }

        var returnUrl = context.Request.Path + context.Request.QueryString;
        context.Response.Redirect($"{_options.ChallengePath}?returnUrl={Uri.EscapeDataString(returnUrl)}");
    }
}

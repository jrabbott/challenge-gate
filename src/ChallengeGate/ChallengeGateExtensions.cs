using System.Diagnostics.CodeAnalysis;
using ChallengeGate.Configuration;
using ChallengeGate.Middleware;
using ChallengeGate.Services;
using ChallengeGate.Validators;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ChallengeGate;

[ExcludeFromCodeCoverage]
public static class ChallengeGateExtensions
{
    public static IServiceCollection AddChallengeGate(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ChallengeOptions>(configuration.GetSection(ChallengeOptions.SectionName));
        
        // Register the concrete implementation and then forward the interfaces to it.
        // This ensures that a single instance of ChallengeGateService is shared across all its interfaces.
        services.AddSingleton<ChallengeGateService>();
        services.AddSingleton<IPasswordMatcher>(sp => sp.GetRequiredService<ChallengeGateService>());
        services.AddSingleton<IChallengeGateAuthenticator>(sp => sp.GetRequiredService<ChallengeGateService>());
        
        services.AddDataProtection();
        services.AddValidatorsFromAssemblyContaining<ChallengeViewModelValidator>();

        // Ensure controllers and views in this Razor Class Library are discovered by the host application.
        services.AddControllersWithViews()
            .AddApplicationPart(typeof(ChallengeGateExtensions).Assembly);

        return services;
    }

    public static IApplicationBuilder UseChallengeGate(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ChallengeMiddleware>();
    }

    public static IEndpointRouteBuilder MapChallengeGate(this IEndpointRouteBuilder endpoints)
    {
        var options = endpoints.ServiceProvider.GetRequiredService<IOptions<ChallengeOptions>>().Value;

        // Map the challenge controller to the configured path
        endpoints.MapControllerRoute(
            name: ChallengeGateConstants.RouteName,
            pattern: options.ChallengePath.TrimStart('/'),
            defaults: new { controller = ChallengeGateConstants.ControllerName, action = ChallengeGateConstants.ActionName });

        return endpoints;
    }
}

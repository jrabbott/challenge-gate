using System.Diagnostics.CodeAnalysis;

namespace ChallengeGate.Configuration;

[ExcludeFromCodeCoverage]
public class ChallengeOptions
{
    public const string SectionName = "ChallengeGate";

    public bool Enabled { get; set; }
    public string? Password { get; set; }
    public string CookieName { get; set; } = ChallengeGateConstants.Defaults.CookieName;
    public int CookieExpirationMinutes { get; set; } = 60 * 24 * 7;
    public string ChallengePath { get; set; } = ChallengeGateConstants.Defaults.ChallengePath;
    public string Layout { get; set; } = ChallengeGateConstants.Defaults.Layout;
    public string Title { get; set; } = ChallengeGateConstants.Defaults.Title;
    public List<string> BypassPaths { get; set; } =
    [
        "/lib",
        "/css",
        "/js",
        "/assets",
        "/favicon.ico"
    ];
}

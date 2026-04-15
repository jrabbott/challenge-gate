using System.Diagnostics.CodeAnalysis;

namespace ChallengeGate.ViewModels;

[ExcludeFromCodeCoverage]
public class ChallengeViewModel
{
    public string? Password { get; set; }

    public string? ReturnUrl { get; set; }
    
    public string Layout { get; set; } = ChallengeGateConstants.Defaults.Layout;
    
    public string Title { get; set; } = ChallengeGateConstants.Defaults.Title;
}
